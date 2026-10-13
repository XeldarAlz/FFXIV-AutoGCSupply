using AutoGcSupply.Core.External;
using AutoGcSupply.Core.Shopper;
using AutoGcSupply.Core.Tasks.Model;

namespace AutoGcSupply.Core.Tasks;

internal static class ShopperProbe
{
    public static ShopperState Read()
    {
        var api = AutoShopperApi.Instance;
        return ShopperStateRule.Classify(ExternalPlugins.IsInstalled(ExternalPlugin.AutoShopper), api.GatesPresent(), api.ApiVersion());
    }
}
