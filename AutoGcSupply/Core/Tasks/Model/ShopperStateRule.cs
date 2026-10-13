using AutoGcSupply.Core.Shopper.Model;

namespace AutoGcSupply.Core.Tasks.Model;

internal static class ShopperStateRule
{
    public static ShopperState Classify(bool installed, bool gatesPresent, int apiVersion)
    {
        if (apiVersion > ShopperContract.ApiVersion)
        {
            return ShopperState.Newer;
        }

        if (gatesPresent && apiVersion == ShopperContract.ApiVersion)
        {
            return ShopperState.Available;
        }

        return installed ? ShopperState.Outdated : ShopperState.Missing;
    }
}
