using AutoGcSupply.Core.Shopper.Model;

namespace AutoGcSupply.Core.Tasks.Model;

internal struct ShopperWatch
{
    public const long StallLimitMs = 1_200_000;
    public const long TotalLimitMs = 10_800_000;

    private readonly long startedAtMs;
    private long movedAtMs;
    private long revision;
    private ShopperRunState state;
    private ShopperRunPhase phase;
    private bool paused;

    public ShopperWatch(long nowMs)
    {
        startedAtMs = nowMs;
        movedAtMs = nowMs;
        revision = -1;
    }

    public void Observe(in ShopperRunView view, long nowMs)
    {
        if (view.Paused || view.Revision != revision || view.State != state || view.Phase != phase)
        {
            movedAtMs = nowMs;
        }

        revision = view.Revision;
        state = view.State;
        phase = view.Phase;
        paused = view.Paused;
    }

    public readonly bool Stalled(long nowMs) => !paused && nowMs - movedAtMs >= StallLimitMs;

    public readonly bool Overdue(long nowMs) => nowMs - startedAtMs >= TotalLimitMs;
}
