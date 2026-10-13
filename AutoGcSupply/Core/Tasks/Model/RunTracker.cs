using System.Collections.Immutable;

namespace AutoGcSupply.Core.Tasks.Model;

internal sealed class RunTracker
{
    private RunModel published = RunModel.Idle;
    private bool dirty;
    private int revision;
    private RunStep step = RunStep.Idle;
    private string status = string.Empty;
    private bool dryRun;
    private string shopperStatus = string.Empty;
    private long confirmedSpend;
    private RunRow[] rows = [];
    private NeedsPlayerReason needsPlayer = NeedsPlayerReason.None;
    private string needsPlayerDetail = string.Empty;
    private RunEnd end = RunEnd.None;
    private RunSummary summary;

    public RunModel Current
    {
        get
        {
            if (!dirty)
            {
                return published;
            }

            dirty = false;
            published = new RunModel
            {
                Revision = revision,
                Step = step,
                Status = status,
                DryRun = dryRun,
                ShopperStatus = shopperStatus,
                ConfirmedSpend = confirmedSpend,
                Rows = ImmutableArray.Create(rows),
                NeedsPlayer = needsPlayer,
                NeedsPlayerDetail = needsPlayerDetail,
                End = end,
                Summary = summary,
            };
            return published;
        }
    }

    public RunStep Step => step;

    public RunEnd End => end;

    public void Begin(RunRow[] initialRows, string initialStatus)
    {
        step = RunStep.Reading;
        status = initialStatus;
        dryRun = false;
        shopperStatus = string.Empty;
        confirmedSpend = 0;
        rows = initialRows;
        needsPlayer = NeedsPlayerReason.None;
        needsPlayerDetail = string.Empty;
        end = RunEnd.None;
        summary = default;
        Touch();
    }

    public void Reset()
    {
        Begin([], string.Empty);
        step = RunStep.Idle;
    }

    public void SetStep(RunStep nextStep, string nextStatus)
    {
        if (step == nextStep && status == nextStatus)
        {
            return;
        }

        step = nextStep;
        status = nextStatus;
        Touch();
    }

    public void SetStatus(string nextStatus)
    {
        if (status == nextStatus)
        {
            return;
        }

        status = nextStatus;
        Touch();
    }

    public void SetDryRun(bool nextDryRun)
    {
        if (dryRun == nextDryRun)
        {
            return;
        }

        dryRun = nextDryRun;
        Touch();
    }

    public void SetShopper(string nextStatus, long nextConfirmedSpend)
    {
        if (shopperStatus == nextStatus && confirmedSpend == nextConfirmedSpend)
        {
            return;
        }

        shopperStatus = nextStatus;
        confirmedSpend = nextConfirmedSpend;
        Touch();
    }

    public void SetRow(int index, in RunRow row)
    {
        if (rows[index] == row)
        {
            return;
        }

        rows[index] = row;
        Touch();
    }

    public void NeedPlayer(NeedsPlayerReason reason, string detail)
    {
        needsPlayer = reason;
        needsPlayerDetail = detail;
        Touch();
    }

    public void Finish(RunEnd runEnd, in RunSummary runSummary, string finalStatus)
    {
        step = RunStep.Done;
        end = runEnd;
        summary = runSummary;
        status = finalStatus;
        Touch();
    }

    private void Touch()
    {
        revision++;
        dirty = true;
    }
}
