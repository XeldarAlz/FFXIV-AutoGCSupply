using AutoGcSupply.Core.Localization;
using AutoGcSupply.Core.Tasks;
using AutoGcSupply.Core.Tasks.Model;
using Dalamud.Bindings.ImGui;
using System.Numerics;

namespace AutoGcSupply.Windows.Sections;

internal static class ReadyState
{
    public enum Kind { SetupNeeded, Ready, Blocked, Running, Paused }

    public readonly record struct Info(Kind Kind, Vector4 Accent, Vector4 AccentSoft);

    private static int cachedFrame = -1;
    private static Info cached;

    public static Info Resolve(GcRunController controller)
    {
        var frame = ImGui.GetFrameCount();
        if (frame == cachedFrame)
        {
            return cached;
        }

        cached = Compute(controller);
        cachedFrame = frame;
        return cached;
    }

    private static Info Compute(GcRunController controller)
    {
        if (controller.Running)
        {
            return controller.Paused
                ? new Info(Kind.Paused, Styling.AccentAmber, Styling.AccentAmberSoft)
                : new Info(Kind.Running, Styling.AccentBlue, Styling.AccentBlueSoft);
        }

        return controller.Today.StartBlock switch
        {
            StartBlock.None => new Info(Kind.Ready, Styling.AccentMint, Styling.AccentMintSoft),
            StartBlock.SetupNeeded => new Info(Kind.SetupNeeded, Styling.AccentRose, Styling.AccentRoseSoft),
            _ => new Info(Kind.Blocked, Styling.AccentNebula, Styling.AccentGlowSoft),
        };
    }

    public static bool CanPause(GcRunController controller) => (controller.Paused && !controller.Settling) || controller.CanPause;

    public static string ShortLabel(Kind kind) => kind switch
    {
        Kind.Running     => Loc.T(L.Shell.StatusRunning),
        Kind.Paused      => Loc.T(L.Shell.StatusPaused),
        Kind.Ready       => Loc.T(L.Shell.StatusReady),
        Kind.Blocked     => Loc.T(L.Shell.StatusBlocked),
        Kind.SetupNeeded => Loc.T(L.Shell.StatusSetupNeeded),
        _                => Loc.T(L.Shell.StatusIdle),
    };

    public static string PhaseLabel(RunPhase phase) => phase switch
    {
        RunPhase.Preparing  => Loc.T(L.Run.PhasePreparing),
        RunPhase.Buying     => Loc.T(L.Running.PhaseBuying),
        RunPhase.Travelling => Loc.T(L.Running.PhaseTravelling),
        RunPhase.HandingIn  => Loc.T(L.Running.PhaseHandingIn),
        RunPhase.Finishing  => Loc.T(L.Running.PhaseFinishing),
        RunPhase.Paused     => Loc.T(L.Run.PhasePaused),
        RunPhase.Stopping   => Loc.T(L.Running.PhaseStopping),
        _                   => Loc.T(L.Run.PhaseStandingBy),
    };
}
