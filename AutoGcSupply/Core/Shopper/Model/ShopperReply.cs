namespace AutoGcSupply.Core.Shopper.Model;

internal enum ShopperReply : byte
{
    Unavailable,
    Valid,
    Malformed,
    SchemaMismatch,
}
