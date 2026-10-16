using System.Text;
using System.Text.Json.Nodes;
using AutoGcSupply.Core.Shopper.Model;

namespace AutoGcSupply.Tests;

public sealed class ShopperRunStatusCacheTests
{
    private const string RunId = "4e1a...";

    [Fact]
    public void UnchangedRevisionReturnsTheCachedViewWithoutAllocating()
    {
        var cache = new ShopperRunStatusCache();
        var json = ShopperFixtures.RunStatus;
        var first = cache.Read(RunId, json);
        cache.Read(RunId, json);

        var before = GC.GetAllocatedBytesForCurrentThread();
        var again = cache.Read(RunId, json);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, allocated);
        Assert.Equal(ShopperReply.Valid, again.Reply);
        Assert.True(first.Lines == again.Lines);
        Assert.True(first.Issues == again.Issues);
    }

    [Fact]
    public void RisingRevisionIsReadAgain()
    {
        var cache = new ShopperRunStatusCache();
        var first = cache.Read(RunId, ShopperFixtures.RunStatus);

        var next = cache.Read(RunId, Edit(root =>
        {
            root["revision"] = 43;
            root["status"] = "Buying Darksteel Ore";
        }));

        Assert.Equal(43, next.Revision);
        Assert.Equal("Buying Darksteel Ore", next.Status);
        Assert.False(first.Lines == next.Lines);
    }

    [Fact]
    public void SameStampKeepsTheViewItWasFirstReadWith()
    {
        var cache = new ShopperRunStatusCache();
        cache.Read(RunId, ShopperFixtures.RunStatus);

        var view = cache.Read(RunId, Edit(root => root["reservedSpend"] = 630));

        Assert.Equal(0, view.ReservedSpend);
    }

    [Fact]
    public void ANewStatusLineUnderTheSameRevisionIsReadAgain()
    {
        var cache = new ShopperRunStatusCache();
        cache.Read(RunId, ShopperFixtures.RunStatus);

        var view = cache.Read(RunId, Edit(root => root["status"] = "Changed without a new revision"));

        Assert.Equal("Changed without a new revision", view.Status);
    }

    [Fact]
    public void AManualPauseTakingOverADutyPauseIsReadAgain()
    {
        var cache = new ShopperRunStatusCache();
        cache.Read(RunId, Edit(root =>
        {
            root["paused"] = true;
            root["pauseReason"] = "InContent";
        }));

        var view = cache.Read(RunId, Edit(root =>
        {
            root["paused"] = true;
            root["pauseReason"] = "Manual";
        }));

        Assert.Equal(ShopperPauseReason.Manual, view.PauseReason);
    }

    [Fact]
    public void ATerminalResultUnderTheSameRevisionIsReadAgain()
    {
        var cache = new ShopperRunStatusCache();
        cache.Read(RunId, ShopperFixtures.RunStatus);

        var view = cache.Read(RunId, Edit(root =>
        {
            root["terminal"] = true;
            root["outcome"] = "Completed";
        }));

        Assert.True(view.Terminal);
        Assert.Equal(ShopperRunOutcome.Completed, view.Outcome);
    }

    [Fact]
    public void APauseUnderTheSameRevisionIsReadAgain()
    {
        var cache = new ShopperRunStatusCache();
        cache.Read(RunId, ShopperFixtures.RunStatus);

        var view = cache.Read(RunId, Edit(root =>
        {
            root["paused"] = true;
            root["pauseReason"] = "InContent";
        }));

        Assert.True(view.Paused);
        Assert.Equal(ShopperPauseReason.InContent, view.PauseReason);
    }

    [Fact]
    public void ANewPhaseUnderTheSameRevisionIsReadAgain()
    {
        var cache = new ShopperRunStatusCache();
        cache.Read(RunId, ShopperFixtures.RunStatus);

        var view = cache.Read(RunId, Edit(root => root["phase"] = "Finishing"));

        Assert.Equal(ShopperRunPhase.Finishing, view.Phase);
    }

    [Fact]
    public void NeedingThePlayerUnderTheSameRevisionIsReadAgain()
    {
        var cache = new ShopperRunStatusCache();
        cache.Read(RunId, ShopperFixtures.RunStatus);

        var view = cache.Read(RunId, Edit(root => root["needsPlayer"] = true));

        Assert.True(view.NeedsPlayer);
    }

    [Fact]
    public void AnotherRunIdIsReadAgain()
    {
        var cache = new ShopperRunStatusCache();
        var first = cache.Read(RunId, ShopperFixtures.RunStatus);

        var other = cache.Read("0b9e...", Edit(root => root["runId"] = "0b9e..."));

        Assert.Equal("0b9e...", other.RunId);
        Assert.False(first.Lines == other.Lines);
    }

    [Fact]
    public void RunThatTurnsUnknownIsReadAgain()
    {
        var cache = new ShopperRunStatusCache();
        cache.Read(RunId, Edit(root => root["revision"] = 0));

        var expired = cache.Read(RunId, $$"""{ "schema": 1, "runId": "{{RunId}}", "known": false, "revision": 0 }""");

        Assert.Equal(ShopperReply.Valid, expired.Reply);
        Assert.False(expired.Known);
    }

    [Fact]
    public void RevisionIsFoundWhateverItsCaseAndPlace()
    {
        var cache = new ShopperRunStatusCache();
        var json = """{ "lines": [ { "itemId": 5057, "quality": "NormalQuality", "requested": 3, "acquired": 1, "missing": 2, "revision": 1 } ], "REVISION": 7, "Known": true, "paused": false, "terminal": false, "needsPlayer": false, "confirmedSpend": 630, "reservedSpend": 0, "SCHEMA": 1 }""";
        var first = cache.Read(RunId, json);

        var again = cache.Read(RunId, json);

        Assert.Equal(7, first.Revision);
        Assert.True(first.Lines == again.Lines);
    }

    [Fact]
    public void FailedReplyIsNotCached()
    {
        var cache = new ShopperRunStatusCache();
        var first = cache.Read(RunId, ShopperFixtures.RunStatus);

        var malformed = cache.Read(RunId, "{ \"schema\": 1, \"known\": true, \"revision\": 42");
        var mismatch = cache.Read(RunId, Edit(root => root["schema"] = 2));
        var recovered = cache.Read(RunId, ShopperFixtures.RunStatus);

        Assert.Equal(ShopperReply.Malformed, malformed.Reply);
        Assert.Equal(ShopperReply.SchemaMismatch, mismatch.Reply);
        Assert.Equal(ShopperReply.Valid, recovered.Reply);
        Assert.False(first.Lines == recovered.Lines);
    }

    [Fact]
    public void KnownRunWithAMissingFieldStaysMalformed()
    {
        var cache = new ShopperRunStatusCache();
        var json = Edit(root => root.Remove("lines"));

        Assert.Equal(ShopperReply.Malformed, cache.Read(RunId, json).Reply);
        Assert.Equal(ShopperReply.Malformed, cache.Read(RunId, json).Reply);
    }

    [Fact]
    public void NoReplyIsUnavailable()
    {
        var view = new ShopperRunStatusCache().Read(RunId, null);

        Assert.Equal(ShopperReply.Unavailable, view.Reply);
        Assert.Empty(view.Lines);
    }

    [Fact]
    public void LargeReplyIsReadWhole()
    {
        var cache = new ShopperRunStatusCache();
        var lines = new JsonArray();
        for (var index = 0; index < 100; index++)
        {
            lines.Add(new JsonObject { ["itemId"] = 5000 + index, ["quality"] = "HighQuality", ["requested"] = 999, ["acquired"] = index, ["missing"] = 999 - index });
        }

        var json = Edit(root =>
        {
            root["lines"] = lines;
            root["status"] = new string('x', 2000);
        });

        var view = cache.Read(RunId, json);

        Assert.True(Encoding.UTF8.GetByteCount(json) > 4096);
        Assert.Equal(ShopperReply.Valid, view.Reply);
        Assert.Equal(100, view.Lines.Length);
        Assert.Equal(new ShopperRunLine(5099, ShopperQuality.HighQuality, 999, 99, 900), view.Lines[99]);
        Assert.True(view.Lines == cache.Read(RunId, json).Lines);
    }

    private static string Edit(Action<JsonObject> edit)
    {
        return ShopperJsonEdit.Edit(ShopperFixtures.RunStatus, edit);
    }
}
