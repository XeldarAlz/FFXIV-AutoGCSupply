using AutoGcSupply.Core.HandIn.Model;
using AutoGcSupply.Core.Inventory.Model;

namespace AutoGcSupply.Tests.HandIn;

public sealed class RequestWindowCheckTests
{
    private const uint Rod = 43273;
    private static readonly HandInTarget target = new(Rod, 8, 1, ItemQuality.High, 3720);

    [Fact]
    public void LoggedHqRodFlowFillsTheRequestAcceptsBaseRewardAndVerifiesActualDelivery()
    {
        var request = Window();
        Assert.Equal(RequestWindowVerdict.NeedsItems, RequestWindowCheck.Judge(request, target));
        RequestOption[] options = [new(0, Rod, ItemQuality.Normal, true), new(1, Rod, ItemQuality.High, true)];
        Assert.Equal(1, RequestWindowCheck.FindOption(options, target));

        request = Window([new(Rod, 1, ItemQuality.High)], enabled: true);
        var verified = RequestWindowCheck.Judge(request, target) == RequestWindowVerdict.Ready;
        Assert.True(verified);
        var reward = new RewardWindowSample(true, [new(4, "Claro Walnut Fishing Rod")], [new(9, 1860)]);
        var judgment = RewardWindowCheck.Judge(reward, "Claro Walnut Fishing Rod", target, 62190, 90000, verified);
        Assert.Equal(RewardWindowVerdict.Matches, judgment.Verdict);
        Assert.True(judgment.BaseSealsShown);

        var before = new DeliverySample(1, 62190, true);
        Assert.Equal(DeliveryVerdict.NotYet, DeliveryCheck.Judge(before, before, target, 90000, final: false));
        Assert.Equal(DeliveryVerdict.Confirmed, DeliveryCheck.Judge(before, new(0, 65910, false), target, 90000, final: true));
        Assert.Equal(DeliveryVerdict.Contradicted, DeliveryCheck.Judge(before, new(1, 64050, false), target, 90000, final: true));
    }

    [Fact]
    public void AcceptsAnAlreadyFilledRequestFromAnotherHelperOnlyAfterCheckingItsContents()
    {
        Assert.Equal(RequestWindowVerdict.Ready, RequestWindowCheck.Judge(Window([new(Rod, 1, ItemQuality.High)], true), target));
        Assert.Equal(RequestWindowVerdict.OtherQuality, RequestWindowCheck.Judge(Window([new(Rod, 1, ItemQuality.Normal)], true), target));
        Assert.Equal(RequestWindowVerdict.OtherItem, RequestWindowCheck.Judge(Window([new(43272, 1, ItemQuality.High)], true), target));
        Assert.Equal(RequestWindowVerdict.OtherQuantity, RequestWindowCheck.Judge(Window([new(Rod, 2, ItemQuality.High)], true), target));
    }

    [Fact]
    public void DoesNotUseAnEnabledButtonAsProofOfCorrectItems()
    {
        Assert.Equal(RequestWindowVerdict.NeedsItems, RequestWindowCheck.Judge(Window(enabled: true), target));
        Assert.Equal(RequestWindowVerdict.Waiting, RequestWindowCheck.Judge(Window([new(Rod, 1, ItemQuality.High)]), target));
        Assert.Equal(RequestWindowVerdict.Waiting, RequestWindowCheck.Judge(RequestWindowState.Unreadable, target));
    }

    [Fact]
    public void RefusesChangedRequestsAndExtraSlotsBeforeFilling()
    {
        Assert.Equal(RequestWindowVerdict.OtherRequest, RequestWindowCheck.Judge(Window() with { Slots = [new(43272, 1, false)] }, target));
        Assert.Equal(RequestWindowVerdict.OtherRequest, RequestWindowCheck.Judge(Window() with { Slots = [new(Rod, 2, false)] }, target));
        Assert.Equal(RequestWindowVerdict.OtherRequest, RequestWindowCheck.Judge(Window() with { Slots = [new(Rod, 1, false), new(43272, 1, false)] }, target));
        Assert.Equal(RequestWindowVerdict.OtherQuality, RequestWindowCheck.Judge(Window() with { Slots = [new(Rod, 1, true)] }, target with { Quality = ItemQuality.Normal }));
    }

    [Fact]
    public void ChecksTheWholeQuantityAcrossStacksWithoutMixingQuality()
    {
        var multiple = target with { Requested = 3 };
        var request = new RequestWindowState(true, [new(Rod, 3, false)], [new(Rod, 1, ItemQuality.High), new(Rod, 2, ItemQuality.High)], true);
        Assert.Equal(RequestWindowVerdict.Ready, RequestWindowCheck.Judge(request, multiple));
        Assert.Equal(RequestWindowVerdict.NeedsItems, RequestWindowCheck.Judge(request with { Offered = [new(Rod, 2, ItemQuality.High)] }, multiple));
        Assert.Equal(RequestWindowVerdict.OtherQuality, RequestWindowCheck.Judge(request with { Offered = [new(Rod, 1, ItemQuality.High), new(Rod, 2, ItemQuality.Normal)] }, multiple));
        Assert.Equal(RequestWindowVerdict.Waiting, RequestWindowCheck.Judge(request with { Offered = [new(Rod, 0, ItemQuality.High)] }, multiple));
    }

    [Fact]
    public void MatchesOptionsByItemQualityAndBagsRegardlessOfTheirOrder()
    {
        RequestOption[] options =
        [
            new(0, Rod, ItemQuality.High, false),
            new(1, 43272, ItemQuality.High, true),
            new(2, Rod, ItemQuality.Normal, true),
            new(3, Rod, ItemQuality.High, true),
        ];
        Assert.Equal(3, RequestWindowCheck.FindOption(options, target));
        Assert.Equal(2, RequestWindowCheck.FindOption(options, target with { Quality = ItemQuality.Normal }));
        Assert.Equal(-1, RequestWindowCheck.FindOption(options.AsSpan(0, 3), target));
        Assert.Equal(-1, RequestWindowCheck.FindOption([], target));
    }

    [Fact]
    public void EmptyHandInInventoryIsNotProofOfQualityOrDelivery()
    {
        Assert.Equal(RequestWindowVerdict.NeedsItems, RequestWindowCheck.CheckOffered([], target));
        Assert.Equal(RequestWindowVerdict.OtherRequest, RequestWindowCheck.CheckOffered([], default));
    }

    [Fact]
    public void ProvisioningSelectsNormalQualityAndTheRequestedQuantity()
    {
        var miner = new HandInTarget(43996, 16, 20, ItemQuality.Normal, 2790);
        var request = new RequestWindowState(true, [new(43996, 20, false)], [new(43996, 20, ItemQuality.Normal)], true);
        Assert.Equal(RequestWindowVerdict.Ready, RequestWindowCheck.Judge(request, miner));
        Assert.Equal(RequestWindowVerdict.OtherQuality, RequestWindowCheck.Judge(request with { Offered = [new(43996, 20, ItemQuality.High)] }, miner));
    }

    private static RequestWindowState Window(RequestStack[]? offered = null, bool enabled = false)
        => new(true, [new(Rod, 1, false)], offered ?? [], enabled);
}
