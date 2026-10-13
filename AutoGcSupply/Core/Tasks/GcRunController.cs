using AutoGcSupply.Core.Ipc;
using AutoGcSupply.Core.Requests.Model;
using AutoGcSupply.Core.Tasks.Model;
using clib.Services;

namespace AutoGcSupply.Core.Tasks;

internal sealed class GcRunController
{
    public PauseReason PauseReason { get; private set; } = PauseReason.None;

    public bool Paused => PauseReason != PauseReason.None;

    public bool Running => Svc.Automation.Running || Paused;

    public RunPhase Phase
    {
        get
        {
            if (Paused)
            {
                return RunPhase.Paused;
            }

            return Running ? RunPhase.Preparing : RunPhase.Idle;
        }
    }

    public string Status => PauseReason switch
    {
        PauseReason.InContent => "Paused while you are in content",
        PauseReason.Manual => "Paused",
        _ => Svc.Automation.CurrentTask?.Status ?? "Idle",
    };

    public TodayModel Today { get; private set; } = TodayModel.Empty;

    public RunModel Run { get; private set; } = RunModel.Idle;

    public void Refresh()
    {
    }

    public void SetIncluded(byte classJobId, bool included)
    {
    }

    public void SetAllIncluded(SupplyMissionKind kind, bool included)
    {
    }

    public void SetSpendCap(long gil)
    {
    }

    public void UseSuggestedSpendCap()
    {
    }

    public void Dismiss()
    {
    }

    public void OpenAutoShopper()
    {
    }

    public void Start()
    {
        if (Running)
        {
            RunLog.Info("Start ignored: a run is already running.");
            return;
        }

        RunLog.Info("Not implemented yet.");
    }

    public void Stop()
    {
        if (!Running)
        {
            return;
        }

        PauseReason = PauseReason.None;
        StopAutomation();
        RunLog.Info("Stop requested; the run is cleared.");
    }

    public void StopForAlert() => Stop();

    public void Shutdown() => Stop();

    public void Pause(PauseReason reason)
    {
        if (reason == PauseReason.None || !Running)
        {
            return;
        }

        if (Paused)
        {
            if (reason == PauseReason.Manual && PauseReason == PauseReason.InContent)
            {
                PauseReason = PauseReason.Manual;
                RunLog.Info("Auto-pause promoted to a manual pause; leaving content will no longer resume.");
            }

            return;
        }

        PauseReason = reason;
        StopAutomation();
        RunLog.Info($"Run paused ({reason}).");
    }

    public void Resume()
    {
        if (!Paused)
        {
            return;
        }

        PauseReason = PauseReason.None;
        RunLog.Info("Resuming the run.");
    }

    public void TogglePause()
    {
        if (Paused)
        {
            Resume();
            return;
        }

        Pause(PauseReason.Manual);
    }

    private static void StopAutomation()
    {
        if (!Svc.Automation.Running)
        {
            return;
        }

        Svc.Automation.Stop();
        NavmeshIPC.Instance.Stop();
    }
}
