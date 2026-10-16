using AutoGcSupply.Core.Character;
using AutoGcSupply.Core.HandIn.Model;
using AutoGcSupply.Core.Inventory.Model;
using AutoGcSupply.Core.Tasks;
using ECommons;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AutoGcSupply.Core.HandIn;

internal sealed partial class HandInDriver : AutoCommon
{
    public const int WatchdogMs = 1_200_000;
    public const int CleanupBoundMs = 30_000;
    public const int NoTarget = RowMatch.NoTarget;

    private const int PollMs = 100;

    private static readonly Func<bool> unloaded = static () => ECommonsMain.Disposed;

    private readonly Func<bool> cancelled;
    private readonly HandInTarget[] targets;
    private readonly HandInRowResult[] rows;
    private readonly OwnedCount[] stocks;
    private readonly SealCapPolicy sealCapPolicy;
    private readonly CancellationToken cancellation;

    private PersonnelOfficer officer;
    private byte grandCompany;
    private int sealCap;

    public HandInDriver(HandInRequest request, CancellationToken cancellation = default)
    {
        targets = (HandInTarget[])request.Targets.Clone();
        rows = new HandInRowResult[targets.Length];
        Array.Fill(rows, HandInRowResult.Pending);
        stocks = new OwnedCount[targets.Length];
        sealCapPolicy = request.SealCapPolicy;
        this.cancellation = cancellation;
        cancelled = () => CancelToken.IsCancellationRequested;
    }

    public HandInStep Step { get; private set; } = HandInStep.Starting;

    public int CurrentTargetIndex { get; private set; } = NoTarget;

    public int Revision { get; private set; }

    public ReadOnlySpan<HandInTarget> Targets => targets;

    public ReadOnlySpan<HandInRowResult> Rows => rows;

    public HandInResult Result { get; private set; } = HandInResult.Running;

    public bool Settled => Step == HandInStep.Finished;

    protected override async Task Execute()
    {
        using var forwardedCancellation = cancellation.Register(Cancel);
        var halt = Halt.None;
        try
        {
            halt = await Run();
        }
        catch (OperationCanceledException)
        {
            SettleActiveRow();
        }
        catch (Exception exception)
        {
            RunLog.Error(exception, "Hand-in: faulted");
            halt = new Halt(HandInPauseReason.Fault, exception.Message);
        }

        await LeaveOfficer();
        Finish(halt);
    }

    private async Task<Halt> Run()
    {
        SkipInvalidTargets();
        Diag($"Hand-in: {targets.Length} target(s), seal cap policy {sealCapPolicy}: {DescribeTargets()}");
        if (!AnyPending())
        {
            Diag("Hand-in: nothing to hand in");
            return Halt.None;
        }

        grandCompany = HandInReads.GrandCompany();
        if (!PersonnelOfficers.TryFor(grandCompany, out officer))
        {
            return new Halt(HandInPauseReason.NotInGrandCompany, $"the character belongs to Grand Company {grandCompany}");
        }

        sealCap = (int)CharacterReader.SealCap();
        var halt = await ReachOfficer();
        if (Interrupted(halt))
        {
            return halt;
        }

        halt = await OpenSupplyList();
        if (!Interrupted(halt))
        {
            halt = await WorkTabs();
        }

        return halt;
    }

    private void SkipInvalidTargets()
    {
        for (var targetIndex = 0; targetIndex < targets.Length; targetIndex++)
        {
            if (targets[targetIndex].IsValid)
            {
                continue;
            }

            SetRow(targetIndex, rows[targetIndex] with { Status = HandInRowStatus.Skipped, Match = RowMatchStatus.InvalidTarget });
            Warn($"Hand-in: refusing a malformed target {targets[targetIndex]}");
        }
    }

    private void Finish(in Halt halt)
    {
        if (CurrentTargetIndex != NoTarget && rows[CurrentTargetIndex].Status == HandInRowStatus.Delivering)
        {
            MarkActiveRow(deliverPressed ? HandInRowStatus.Unconfirmed : HandInRowStatus.Pending);
        }

        var delivered = 0;
        var sealsGained = 0;
        var sealsLost = 0;
        for (var targetIndex = 0; targetIndex < rows.Length; targetIndex++)
        {
            var row = rows[targetIndex];
            if (row.Status == HandInRowStatus.Delivered)
            {
                delivered++;
                sealsGained += row.SealsGained;
                sealsLost += row.SealsLost;
            }
        }

        var outcome = CancelToken.IsCancellationRequested ? HandInOutcome.Cancelled
            : halt.Stopped ? HandInOutcome.Paused
            : delivered == rows.Length ? HandInOutcome.Completed
            : HandInOutcome.Partial;
        Step = HandInStep.Finished;
        CurrentTargetIndex = NoTarget;
        Result = new HandInResult(outcome, halt.Reason, halt.Detail, halt.RewardWindow, rows, delivered, sealsGained, sealsLost);
        Bump();

        var summary = $"Hand-in: {outcome}, {delivered}/{rows.Length} delivered, {sealsGained} seals gained, {sealsLost} lost to the cap";
        if (halt.Stopped)
        {
            Warn($"{summary}; paused ({halt.Reason}): {halt.Detail}");
            return;
        }

        Diag(summary);
    }

    private bool AnyPending()
    {
        for (var targetIndex = 0; targetIndex < rows.Length; targetIndex++)
        {
            if (rows[targetIndex].Status == HandInRowStatus.Pending)
            {
                return true;
            }
        }

        return false;
    }

    private bool AnyPendingOn(SupplyListTab tab)
    {
        for (var targetIndex = 0; targetIndex < rows.Length; targetIndex++)
        {
            if (rows[targetIndex].Status == HandInRowStatus.Pending && targets[targetIndex].Tab == tab)
            {
                return true;
            }
        }

        return false;
    }

    private string DescribeTargets()
    {
        var builder = new StringBuilder();
        for (var targetIndex = 0; targetIndex < targets.Length; targetIndex++)
        {
            if (builder.Length > 0)
            {
                builder.Append(", ");
            }

            builder.Append(HandInReads.Describe(targets[targetIndex]));
        }

        return builder.ToString();
    }

    private bool Interrupted(in Halt halt) => halt.Stopped || CancelToken.IsCancellationRequested;

    private void EnterStep(HandInStep step, string status)
    {
        Step = step;
        Status = status;
        Bump();
    }

    private void SetRow(int targetIndex, in HandInRowResult row)
    {
        rows[targetIndex] = row;
        Bump();
    }

    private void Bump() => Revision++;

    private readonly record struct Halt(HandInPauseReason Reason, string Detail)
    {
        public static Halt None { get; } = new(HandInPauseReason.None, string.Empty);

        public RewardWindowMismatch RewardWindow { get; init; } = RewardWindowMismatch.None;

        public bool Stopped => Reason != HandInPauseReason.None;
    }
}
