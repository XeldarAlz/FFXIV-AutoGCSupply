using AutoGcSupply.Core.Requests.Model;
using AutoGcSupply.Core.Tasks.Model;
using AutoGcSupply.Core.Value.Model;

namespace AutoGcSupply.Core.Tasks;

internal sealed class GcRunSession
{
    public GcRunSession(RunAuthorization authorization, RunTracker tracker, IRowLabels labels)
    {
        Authorization = authorization;
        Tracker = tracker;
        Labels = labels;
        Plan = authorization.Rows.AsSpan().ToArray();
        Progress = RunPlanRules.Initial(Plan);
        ReadDay = authorization.Day;
    }

    public RunAuthorization Authorization { get; }

    public RunTracker Tracker { get; }

    public IRowLabels Labels { get; }

    public RowDecision[] Plan { get; }

    public RowProgress[] Progress { get; }

    public DayKey ReadDay { get; set; }

    public string ShopperRunId { get; set; } = string.Empty;

    public bool Buying { get; set; }

    public bool ShopperEnded { get; set; }

    public bool StopRequested { get; private set; }

    public long StopRequestedAtMs { get; private set; }

    public bool DidWork { get; set; }

    public bool HandInStarted { get; set; }

    public long GilSpent { get; set; }

    public RunEnd End { get; private set; } = RunEnd.None;

    public string Failure { get; private set; } = string.Empty;

    public bool CanStopShopper => Buying && !StopRequested && ShopperRunId.Length > 0;

    public string ItemName(int rowIndex) => Labels.ItemName(Plan[rowIndex].Request.ItemId);

    public void RequestStop(long nowMs)
    {
        StopRequested = true;
        StopRequestedAtMs = nowMs;
    }

    public void PublishRows()
    {
        for (var index = 0; index < Plan.Length; index++)
        {
            Tracker.SetRow(index, RunRow.For(Authorization.Rows.ItemRef(index), Progress[index], RunText.Detail(Progress[index], Plan[index])));
        }
    }

    public void Conclude(RunEnd end)
    {
        End = end;
    }

    public void Fail(string failure)
    {
        Failure = failure;
        End = RunEnd.Failed;
    }

    public void NeedPlayer(NeedsPlayerReason reason, string detail)
    {
        End = RunEnd.NeedsPlayer;
        Tracker.NeedPlayer(reason, detail);
    }
}
