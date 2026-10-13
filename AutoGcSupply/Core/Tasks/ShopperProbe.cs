using AutoGcSupply.Core.External;
using AutoGcSupply.Core.Shopper;
using AutoGcSupply.Core.Tasks.Model;

namespace AutoGcSupply.Core.Tasks;

internal static class ShopperProbe
{
    public static ShopperState Read()
    {
        if (AutoShopperApi.Instance.IsAvailable)
        {
            return ShopperState.Available;
        }

        return ExternalPlugins.IsInstalled(ExternalPlugin.AutoShopper) ? ShopperState.Outdated : ShopperState.Missing;
    }
}
