using AutoGcSupply.Core.Shopper.Model;

namespace AutoGcSupply.Core.Tasks.Model;

internal enum ShopperSettlementKind : byte { Running, Unreadable, Continue, Stopped, NeedsPlayer }

internal readonly record struct ShopperSettlement(ShopperSettlementKind Kind, NeedsPlayerReason Reason)
{
    public static readonly ShopperSettlement Running = new(ShopperSettlementKind.Running, NeedsPlayerReason.None);
    public static readonly ShopperSettlement Unreadable = new(ShopperSettlementKind.Unreadable, NeedsPlayerReason.None);
    public static readonly ShopperSettlement Continue = new(ShopperSettlementKind.Continue, NeedsPlayerReason.None);
    public static readonly ShopperSettlement Stopped = new(ShopperSettlementKind.Stopped, NeedsPlayerReason.None);

    public static ShopperSettlement NeedsPlayer(NeedsPlayerReason reason) => new(ShopperSettlementKind.NeedsPlayer, reason);
}

internal static class ShopperSettlementRule
{
    public static ShopperSettlement Classify(in ShopperRunView view, bool stopRequested)
    {
        if (view.Reply != ShopperReply.Valid || !view.Known)
        {
            return ShopperSettlement.Unreadable;
        }

        if (!view.Terminal)
        {
            return ShopperSettlement.Running;
        }

        if (view.Outcome == ShopperRunOutcome.NeedsReconciliation)
        {
            return ShopperSettlement.NeedsPlayer(NeedsPlayerReason.ShopperUncertainPurchase);
        }

        if (view.NeedsPlayer)
        {
            return ShopperSettlement.NeedsPlayer(view.Outcome == ShopperRunOutcome.Stopped ? NeedsPlayerReason.ShopperInterrupted : NeedsPlayerReason.ShopperUncertainPurchase);
        }

        if (stopRequested)
        {
            return ShopperSettlement.Stopped;
        }

        if (view.Outcome != ShopperRunOutcome.Stopped || view.Refusal == ShopperRefusal.NothingToBuy)
        {
            return ShopperSettlement.Continue;
        }

        return ShopperSettlement.NeedsPlayer(view.Refusal == ShopperRefusal.None ? NeedsPlayerReason.ShopperStopped : ReasonForRefusal(view.Refusal));
    }

    public static ShopperSettlement LostTrack(bool stopTaken)
        => ShopperSettlement.NeedsPlayer(stopTaken ? NeedsPlayerReason.ShopperLostTrackStopped : NeedsPlayerReason.ShopperLostTrack);

    public static NeedsPlayerReason ReasonForRefusal(ShopperRefusal refusal) => refusal switch
    {
        ShopperRefusal.ConsentRequired or ShopperRefusal.ConsentDenied => NeedsPlayerReason.ShopperConsent,
        ShopperRefusal.NeedsReconciliation => NeedsPlayerReason.ShopperUncertainPurchase,
        ShopperRefusal.InterruptedRun => NeedsPlayerReason.ShopperInterrupted,
        ShopperRefusal.SetupNeeded
            or ShopperRefusal.JournalDamaged
            or ShopperRefusal.JournalWriteFailed
            or ShopperRefusal.Busy
            or ShopperRefusal.ReadingUnavailable
            or ShopperRefusal.BuyingUnavailable => NeedsPlayerReason.ShopperNotReady,
        _ => NeedsPlayerReason.ShopperRefused,
    };

    public static bool OwnedByShopper(NeedsPlayerReason reason)
        => reason is NeedsPlayerReason.ShopperConsent
            or NeedsPlayerReason.ShopperUncertainPurchase
            or NeedsPlayerReason.ShopperInterrupted
            or NeedsPlayerReason.ShopperNotReady
            or NeedsPlayerReason.ShopperStopped
            or NeedsPlayerReason.ShopperLostTrack
            or NeedsPlayerReason.ShopperLostTrackStopped;
}
