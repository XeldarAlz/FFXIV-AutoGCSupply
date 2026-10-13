using AutoGcSupply.Core.Inventory.Model;

namespace AutoGcSupply.Core.HandIn.Model;

internal static class RequestWindowCheck
{
    public static RequestWindowVerdict Judge(in RequestWindowState shown, in HandInTarget target)
    {
        if (!shown.Readable || shown.Slots.Length == 0)
        {
            return RequestWindowVerdict.Waiting;
        }

        // One GC mission requests one item type, even when its quantity exceeds one.
        if (!target.IsValid || shown.Slots.Length != 1 || shown.Slots[0].ItemId != target.ItemId
            || shown.Slots[0].Quantity != target.Requested)
        {
            return RequestWindowVerdict.OtherRequest;
        }

        if (shown.Slots[0].HighQualityRequired && target.Quality != ItemQuality.High)
        {
            return RequestWindowVerdict.OtherQuality;
        }

        var offered = CheckOffered(shown.Offered, target);
        return offered == RequestWindowVerdict.Ready && !shown.HandOverEnabled ? RequestWindowVerdict.Waiting : offered;
    }

    public static RequestWindowVerdict CheckOffered(ReadOnlySpan<RequestStack> offered, in HandInTarget target)
    {
        if (!target.IsValid) return RequestWindowVerdict.OtherRequest;
        long quantity = 0;
        foreach (var stack in offered)
        {
            if (stack.ItemId != target.ItemId) return RequestWindowVerdict.OtherItem;
            if (stack.Quality != target.Quality) return RequestWindowVerdict.OtherQuality;
            if (stack.Quantity <= 0) return RequestWindowVerdict.Waiting;
            quantity += stack.Quantity;
        }

        return quantity > target.Requested ? RequestWindowVerdict.OtherQuantity
            : quantity == target.Requested ? RequestWindowVerdict.Ready : RequestWindowVerdict.NeedsItems;
    }

    public static int FindOption(ReadOnlySpan<RequestOption> options, in HandInTarget target)
    {
        foreach (var option in options)
        {
            if (option.InBags && option.ItemId == target.ItemId && option.Quality == target.Quality)
            {
                return option.Index;
            }
        }

        return -1;
    }
}
