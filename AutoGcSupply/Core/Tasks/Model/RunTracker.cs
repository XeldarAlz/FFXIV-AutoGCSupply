using System.Collections.Immutable;

namespace AutoGcSupply.Core.Tasks.Model;

internal sealed class RunTracker
{
    private RunModel published = RunModel.Idle;
    private bool dirty;
    private int revision;
    private RunStep step = RunStep.Idle;
    private RunStep lastStep = RunStep.Idle;
    private string status = string.Empty;
    private string shopperStatus = string.Empty;
    private bool shopperPaused;
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
                LastStep = lastStep,
                Status = status,
                ShopperStatus = shopperStatus,
                ShopperPaused = shopperPaused,
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

    public bool ShopperPaused => shopperPaused;

    public void Begin(RunRow[] initialRows, string initialStatus)
    {
        step = RunStep.Reading;
        lastStep = RunStep.Reading;
        status = initialStatus;
        shopperStatus = string.Empty;
        shopperPaused = false;
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
        lastStep = RunStep.Idle;
    }

    public void SetStep(RunStep nextStep, string nextStatus)
    {
        if (step == nextStep && status == nextStatus)
        {
            return;
        }

        step = nextStep;
        lastStep = nextStep;
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

    public void SetShopper(string nextStatus, long nextConfirmedSpend, bool nextPaused)
    {
        if (shopperStatus == nextStatus && confirmedSpend == nextConfirmedSpend && shopperPaused == nextPaused)
        {
            return;
        }

        shopperStatus = nextStatus;
        confirmedSpend = nextConfirmedSpend;
        shopperPaused = nextPaused;
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
