using AutoGcSupply.Core.Character;
using AutoGcSupply.Core.Character.Model;
using AutoGcSupply.Core.External;
using AutoGcSupply.Core.Game.Watchers;
using AutoGcSupply.Core.Inventory;
using AutoGcSupply.Core.Inventory.Model;
using AutoGcSupply.Core.Requests;
using AutoGcSupply.Core.Requests.Model;
using AutoGcSupply.Core.Shopper;
using AutoGcSupply.Core.Shopper.Model;
using AutoGcSupply.Core.Tasks.Model;
using AutoGcSupply.Core.Value.Model;
using Dalamud.Game.Inventory.InventoryEventArgTypes;
using Dalamud.Plugin.Services;
using ECommons.DalamudServices;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using ClibServices = clib.Services.Svc;

namespace AutoGcSupply.Core.Tasks;

internal sealed partial class GcRunController
{
    private const long PollIntervalMs = TimeUnits.MillisecondsPerSecond;
    private const long WatchedForMs = 2 * TimeUnits.MillisecondsPerSecond;

    private TodayModel today = TodayModel.Empty;
    private int todayRevision;
    private bool todayDirty = true;
    private long viewedAtMs;
    private long lastPollMs;
    private long keptAliveAtMs;
    private bool bagsChanged;
    private bool loggedIn;
    private bool hasCharacter;
    private CharacterState character;
    private bool inDuty;
    private RequestSnapshot snapshot = RequestSnapshot.Unread(DateTime.UtcNow);
    private bool requestsCurrent;
    private ImmutableArray<OwnedCount> ownedBySlot = ImmutableArray<OwnedCount>.Empty;
    private DecisionSettings settings;
    private long spendCap;
    private ShopperState shopper = ShopperState.Missing;
    private bool shopperDryRun;
    private bool setupReady;
    private int seenQuoteRevision;
    private ShopperQuoteLine[] neededQuote = [];
    private DayKey day;
    private bool dayKnown;
    private bool rolloverLoadWanted;
    private bool quotesOutdated;
    private bool refreshWanted;
    private LoadMark autoLoaded;
    private TimersLoader? loader;

    private void OnUpdate(IFramework framework)
    {
        var now = Environment.TickCount64;
        if (now - viewedAtMs > WatchedForMs)
        {
            KeepQuoteAlive(now);
            return;
        }

        if (now - lastPollMs >= PollIntervalMs)
        {
            lastPollMs = now;
            ReadInputs();
            if (session is null)
            {
                TickQuotes();
                TryLoad();
            }
        }
        else if (bagsChanged)
        {
            RecountBags();
        }

        if (quotes.Book.Revision != seenQuoteRevision)
        {
            seenQuoteRevision = quotes.Book.Revision;
            todayDirty = true;
        }

        if (todayDirty)
        {
            Rebuild();
        }
    }

    private void OnInventoryChanged(IReadOnlyCollection<InventoryEventArgs> events) => bagsChanged = true;

    private void ReadInputs()
    {
        ReadDay(DateTime.UtcNow);
        DropOutdatedQuotes();
        ReadCharacter();
        ReadDuty();
        ReadRequests();
        RecountBags();
        ReadSettings();
        ReadShopper();
    }

    private void ReadDay(DateTime nowUtc)
    {
        var current = DayKey.For(nowUtc);
        if (dayKnown && current == day)
        {
            return;
        }

        if (dayKnown)
        {
            rolloverLoadWanted = true;
            quotesOutdated = true;
            RunLog.Info($"A new supply day began ({current}); the requests are read again.");
        }

        day = current;
        dayKnown = true;
        todayDirty = true;
    }

    private void DropOutdatedQuotes()
    {
        if (!quotesOutdated || session is not null)
        {
            return;
        }

        quotesOutdated = false;
        quotes.Clear();
    }

    private void ReadCharacter()
    {
        var loggedInNow = Svc.ClientState.IsLoggedIn;
        var hasCharacterNow = CharacterReader.TryRead(out var characterNow);
        if (loggedInNow == loggedIn && hasCharacterNow == hasCharacter && (!hasCharacterNow || TodayInputChanges.SameCharacter(characterNow, character)))
        {
            return;
        }

        loggedIn = loggedInNow;
        hasCharacter = hasCharacterNow;
        character = characterNow;
        todayDirty = true;
    }

    private void ReadDuty()
    {
        var inDutyNow = DutyWatcher.InContent();
        if (inDutyNow == inDuty)
        {
            return;
        }

        inDuty = inDutyNow;
        todayDirty = true;
    }

    private void ReadRequests()
    {
        var snapshotNow = GcSupplyReader.Read();
        var currentNow = timersPasses.IsCurrent(snapshotNow.ContentId, day);
        if (currentNow == requestsCurrent && TodayInputChanges.SameRequests(snapshotNow, snapshot))
        {
            return;
        }

        snapshot = snapshotNow;
        requestsCurrent = currentNow;
        todayDirty = true;
    }

    private void RecountBags()
    {
        bagsChanged = false;
        var counts = snapshot.Loaded ? BagCounter.CountBySlot(snapshot.Requests) : [];
        if (TodayInputChanges.SameCounts(counts, ownedBySlot.AsSpan()))
        {
            return;
        }

        ownedBySlot = ImmutableCollectionsMarshal.AsImmutableArray(counts);
        todayDirty = true;
    }

    private void ReadSettings()
    {
        var contentId = hasCharacter ? character.ContentId : 0;
        var settingsNow = new DecisionSettings(
            configuration.IncludeMaxLevelClasses,
            configuration.UseOwnedItems,
            configuration.SupplyQuality,
            configuration.PriceTolerancePercent,
            ExcludedFor(contentId));
        var spendCapNow = configuration.RunSpendCap;
        if (settingsNow == settings && spendCapNow == spendCap)
        {
            return;
        }

        settings = settingsNow;
        spendCap = spendCapNow;
        todayDirty = true;
    }

    private void ReadShopper()
    {
        var shopperNow = ShopperProbe.Read();
        var dryRunNow = shopperNow == ShopperState.Available && AutoShopperApi.Instance.DryRun(whenUnknown: false);
        var setupReadyNow = ExternalPlugins.AllRequiredInstalled();
        if (shopperNow == shopper && dryRunNow == shopperDryRun && setupReadyNow == setupReady)
        {
            return;
        }

        shopper = shopperNow;
        shopperDryRun = dryRunNow;
        setupReady = setupReadyNow;
        todayDirty = true;
    }

    private void TickQuotes()
    {
        if (shopper != ShopperState.Available)
        {
            return;
        }

        if (quotes.Book.Outstanding)
        {
            quotes.Poll();
            return;
        }

        if (quotes.Book.NeedsQuote(neededQuote))
        {
            quotes.Start(neededQuote);
        }
    }

    private void KeepQuoteAlive(long now)
    {
        if (session is not null || !quotes.Book.Outstanding || now - keptAliveAtMs < PollIntervalMs)
        {
            return;
        }

        keptAliveAtMs = now;
        quotes.Poll();
    }

    private void TryLoad()
    {
        if (loader is not null || !hasCharacter || !character.InGrandCompany)
        {
            refreshWanted = false;
            return;
        }

        var mark = new LoadMark(character.ContentId, day);
        var automatic = !requestsCurrent && autoLoaded != mark;
        if (!refreshWanted && !rolloverLoadWanted && !automatic)
        {
            return;
        }

        if (!TimersLoader.PlayerFree())
        {
            refreshWanted = false;
            return;
        }

        refreshWanted = false;
        rolloverLoadWanted = false;
        autoLoaded = mark;
        StartLoader();
    }

    private void StartLoader()
    {
        var task = new TimersLoader(timersPasses, forcePass: true);
        loader = task;
        todayDirty = true;
        try
        {
            ClibServices.Automation.Start(task, OnCompleted: () => _ = Svc.Framework.RunOnFrameworkThread(() => OnLoaderEnded(task)));
            RunLog.Info("Reading today's requests through the Timers window.");
        }
        catch (Exception exception)
        {
            loader = null;
            RunLog.Error(exception, "The Timers loader could not be started");
        }
    }

    private void OnLoaderEnded(TimersLoader task)
    {
        if (!ReferenceEquals(loader, task))
        {
            return;
        }

        loader = null;
        lastPollMs = 0;
        todayDirty = true;
    }

    private void Rebuild()
    {
        todayDirty = false;
        var inputs = new TodayInputs
        {
            LoggedIn = loggedIn,
            HasCharacter = hasCharacter,
            Character = character,
            InDuty = inDuty,
            Snapshot = snapshot,
            RequestsCurrent = requestsCurrent,
            Reading = loader is not null,
            OwnedBySlot = ownedBySlot,
            Settings = settings,
            Shopper = shopper,
            ShopperDryRun = shopperDryRun,
            SpendCap = spendCap,
            SetupReady = setupReady,
            Running = session is not null,
            NowUtc = DateTime.UtcNow,
        };
        today = TodayBuilder.Build(inputs, quotes.Book, labels, ++todayRevision, out neededQuote);
    }

    private readonly record struct LoadMark(ulong ContentId, DayKey Day);
}
