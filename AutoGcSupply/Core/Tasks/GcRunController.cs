using AutoGcSupply.Core.Localization;
using AutoGcSupply.Core.Requests.Model;
using AutoGcSupply.Core.Stats;
using AutoGcSupply.Core.Tasks.Model;
using ECommons.DalamudServices;
using ClibServices = clib.Services.Svc;

namespace AutoGcSupply.Core.Tasks;

internal sealed partial class GcRunController
{
    private const string AutoShopperCommand = "/ashop";

    private readonly Configuration configuration;
    private readonly RunHistory history;
    private readonly QuoteTracker quotes = new();
    private readonly RowLabels labels = new();
    private readonly RunTracker tracker = new();

    public GcRunController(Configuration configuration, RunHistory history)
    {
        this.configuration = configuration;
        this.history = history;
        Svc.Framework.Update += OnUpdate;
        Svc.GameInventory.InventoryChangedRaw += OnInventoryChanged;
    }

    public PauseReason PauseReason { get; private set; } = PauseReason.None;

    public bool Paused => PauseReason != PauseReason.None;

    public bool Running => session is not null;

    public bool CanPause => !Paused && session is { End: RunEnd.None, Buying: false };

    public RunPhase Phase
    {
        get
        {
            if (session is not { } active)
            {
                return RunPhase.Idle;
            }

            if (Paused)
            {
                return RunPhase.Paused;
            }

            if (active.StopRequested)
            {
                return RunPhase.Stopping;
            }

            return tracker.Step switch
            {
                RunStep.Buying => RunPhase.Buying,
                RunStep.Travelling => RunPhase.Travelling,
                RunStep.HandingIn => RunPhase.HandingIn,
                RunStep.Finishing or RunStep.Done => RunPhase.Finishing,
                _ => RunPhase.Preparing,
            };
        }
    }

    public string Status => session is null ? Loc.T(L.Shell.StatusIdle) : tracker.Current.Status;

    public TodayModel Today
    {
        get
        {
            viewedAtMs = Environment.TickCount64;
            return today;
        }
    }

    public RunModel Run => tracker.Current;

    public void Refresh()
    {
        if (session is not null)
        {
            RunLog.Info("Refresh ignored: a run is reading the requests itself.");
            return;
        }

        quotes.Clear();
        refreshWanted = true;
        lastPollMs = 0;
        todayDirty = true;
        RunLog.Info("Refresh: reading the requests and pricing them again.");
    }

    public void SetIncluded(byte classJobId, bool included)
    {
        var contentId = Svc.PlayerState.ContentId;
        if (contentId == 0 || !SupplyClasses.Contains(classJobId))
        {
            return;
        }

        StoreExcluded(contentId, ExcludedFor(contentId).With(classJobId, !included));
    }

    public void SetAllIncluded(SupplyMissionKind kind, bool included)
    {
        var contentId = Svc.PlayerState.ContentId;
        if (contentId == 0)
        {
            return;
        }

        var mask = ExcludedFor(contentId);
        var order = SupplyClasses.DisplayOrder;
        for (var index = 0; index < order.Length; index++)
        {
            if (SupplyClasses.KindOf(order[index]) == kind)
            {
                mask = mask.With(order[index], !included);
            }
        }

        StoreExcluded(contentId, mask);
    }

    public void SetSpendCap(long gil)
    {
        var clamped = SpendCapRule.Clamp(gil);
        if (clamped == configuration.RunSpendCap)
        {
            return;
        }

        configuration.RunSpendCap = clamped;
        configuration.SaveDebounced();
        ReadSettings();
    }

    public void UseSuggestedSpendCap() => SetSpendCap(today.SuggestedSpendCap);

    public void Dismiss()
    {
        if (session is not null || tracker.End == RunEnd.None)
        {
            return;
        }

        tracker.Reset();
    }

    public void OpenAutoShopper() => Plugin.CommandManager.ProcessCommand(AutoShopperCommand);

    public void Shutdown()
    {
        Interrupt();
        if (loader is not null)
        {
            loader = null;
            ClibServices.Automation.Stop();
        }

        Svc.Framework.Update -= OnUpdate;
        Svc.GameInventory.InventoryChangedRaw -= OnInventoryChanged;
    }

    private ClassMask ExcludedFor(ulong contentId)
        => configuration.ExcludedClassesByCharacter.TryGetValue(contentId, out var bits) ? new ClassMask(bits) : ClassMask.None;

    private void StoreExcluded(ulong contentId, ClassMask mask)
    {
        if (mask == ExcludedFor(contentId))
        {
            return;
        }

        if (mask.Bits == 0)
        {
            configuration.ExcludedClassesByCharacter.Remove(contentId);
        }
        else
        {
            configuration.ExcludedClassesByCharacter[contentId] = mask.Bits;
        }

        configuration.SaveDebounced();
        ReadSettings();
    }
}
