namespace AutoGcSupply.Core.Shopper.Model;

internal readonly record struct ShopperAcceptance(ShopperReply Reply, string Id, ShopperRefusal Refusal, string RefusalName, string Message)
{
    public bool Accepted => Reply == ShopperReply.Valid && Refusal == ShopperRefusal.None;

    public static ShopperAcceptance Failed(ShopperReply reply)
    {
        return new ShopperAcceptance(reply, string.Empty, ShopperRefusal.None, string.Empty, string.Empty);
    }
}
