using AutoGcSupply.Core.Ipc;
using AutoGcSupply.Core.Shopper.Model;
using Dalamud.Plugin.Ipc;
using ECommons.DalamudServices;

namespace AutoGcSupply.Core.Shopper;

internal sealed class AutoShopperApi
{
    private const string GatePrefix = "AutoShopper.Api.";
    private const string ApiVersionFailed = GcSupplyConstants.LogPrefix + " Auto Shopper ApiVersion failed";
    private const string DryRunFailed = GcSupplyConstants.LogPrefix + " Auto Shopper DryRun failed";
    private const string QuoteStartFailed = GcSupplyConstants.LogPrefix + " Auto Shopper QuoteStart failed";
    private const string QuoteResultFailed = GcSupplyConstants.LogPrefix + " Auto Shopper QuoteResult failed";
    private const string StartRunFailed = GcSupplyConstants.LogPrefix + " Auto Shopper StartRun failed";
    private const string RunStatusFailed = GcSupplyConstants.LogPrefix + " Auto Shopper RunStatus failed";
    private const string StopRunFailed = GcSupplyConstants.LogPrefix + " Auto Shopper StopRun failed";

    private static AutoShopperApi? instance;

    private readonly ICallGateSubscriber<int> apiVersion;
    private readonly ICallGateSubscriber<bool> dryRun;
    private readonly ICallGateSubscriber<string, bool> stopRun;
    private readonly JsonGate quoteStart;
    private readonly JsonGate quoteResult;
    private readonly JsonGate startRun;
    private readonly JsonGate runStatus;
    private readonly Func<int> apiVersionCall;
    private readonly Func<bool> dryRunCall;
    private readonly ShopperRunStatusCache runStatusCache = new();

    private AutoShopperApi()
    {
        var pluginInterface = Svc.PluginInterface;
        apiVersion = pluginInterface.GetIpcSubscriber<int>(GatePrefix + "ApiVersion");
        dryRun = pluginInterface.GetIpcSubscriber<bool>(GatePrefix + "DryRun");
        stopRun = pluginInterface.GetIpcSubscriber<string, bool>(GatePrefix + "StopRun");
        quoteStart = new JsonGate(pluginInterface.GetIpcSubscriber<string, string>(GatePrefix + "QuoteStart"), QuoteStartFailed);
        quoteResult = new JsonGate(pluginInterface.GetIpcSubscriber<string, string>(GatePrefix + "QuoteResult"), QuoteResultFailed);
        startRun = new JsonGate(pluginInterface.GetIpcSubscriber<string, string>(GatePrefix + "StartRun"), StartRunFailed);
        runStatus = new JsonGate(pluginInterface.GetIpcSubscriber<string, string>(GatePrefix + "RunStatus"), RunStatusFailed);

        apiVersionCall = apiVersion.InvokeFunc;
        dryRunCall = dryRun.InvokeFunc;
    }

    public static AutoShopperApi Instance => instance ??= new AutoShopperApi();

    public bool IsAvailable => GatesPresent() && ApiVersion() == ShopperContract.ApiVersion;

    public int ApiVersion()
        => IpcGate.Invoke(apiVersion.HasFunction, apiVersionCall, 0, ApiVersionFailed);

    public bool DryRun(bool whenUnknown)
        => IpcGate.Invoke(dryRun.HasFunction, dryRunCall, whenUnknown, DryRunFailed);

    public ShopperAcceptance QuoteStart(ReadOnlySpan<ShopperQuoteLine> lines)
    {
        var acceptance = ShopperReplies.ParseQuoteStart(quoteStart.Invoke(ShopperRequests.BuildQuoteStart(lines)));
        WarnUnreadable(acceptance.Reply, nameof(QuoteStart));
        return acceptance;
    }

    public ShopperQuoteView QuoteResult(string quoteId)
    {
        var quote = ShopperReplies.ParseQuoteResult(quoteResult.Invoke(quoteId));
        WarnUnreadable(quote.Reply, nameof(QuoteResult));
        return quote;
    }

    public ShopperAcceptance StartRun(string callerRunId, string label, long maxSpend, bool returnHome, ReadOnlySpan<ShopperLine> lines)
    {
        var acceptance = ShopperReplies.ParseStartRun(startRun.Invoke(ShopperRequests.BuildStartRun(callerRunId, label, maxSpend, returnHome, lines)));
        WarnUnreadable(acceptance.Reply, nameof(StartRun));
        return acceptance;
    }

    public ShopperRunView RunStatus(string runId)
    {
        var view = runStatusCache.Read(runId, runStatus.Invoke(runId));
        WarnUnreadable(view.Reply, nameof(RunStatus));
        return view;
    }

    public bool StopRun(string runId)
        => IpcGate.Invoke(stopRun.HasFunction, () => stopRun.InvokeFunc(runId), false, StopRunFailed);

    private bool GatesPresent()
    {
        return apiVersion.HasFunction
            && dryRun.HasFunction
            && quoteStart.HasFunction
            && quoteResult.HasFunction
            && startRun.HasFunction
            && runStatus.HasFunction
            && stopRun.HasFunction;
    }

    private static void WarnUnreadable(ShopperReply reply, string gate)
    {
        if (reply is ShopperReply.Valid or ShopperReply.Unavailable)
        {
            return;
        }

        RunLog.Warning($"Auto Shopper {gate} reply was not usable: {reply}");
    }

    private sealed class JsonGate
    {
        private readonly ICallGateSubscriber<string, string> gate;
        private readonly Func<string?> call;
        private readonly string failure;
        private string argument = string.Empty;

        public JsonGate(ICallGateSubscriber<string, string> gate, string failure)
        {
            this.gate = gate;
            this.failure = failure;
            call = InvokeWithArgument;
        }

        public bool HasFunction => gate.HasFunction;

        public string? Invoke(string value)
        {
            argument = value;
            return IpcGate.Invoke(gate.HasFunction, call, null, failure);
        }

        private string? InvokeWithArgument()
        {
            return gate.InvokeFunc(argument);
        }
    }
}
