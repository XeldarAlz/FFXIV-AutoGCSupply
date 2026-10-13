namespace AutoGcSupply.Core.Stats;

internal sealed class RunHistory
{
    private readonly List<RunRecord> records = [];

    public IReadOnlyList<RunRecord> Records => records;

    public int Revision { get; private set; }

    public void Append(RunRecord record)
    {
        records.Add(record);
        Revision++;
    }

    public void Clear()
    {
        records.Clear();
        Revision++;
    }
}
