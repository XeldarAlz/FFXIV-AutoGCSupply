using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System.IO;

namespace AutoGcSupply.Core.Stats;

internal sealed class RunHistory
{
    public const int MaxRecords = 200;

    private const int FileVersion = 1;
    private const string FileName = "run-history.json";
    private const string TemporarySuffix = ".tmp";
    private const string LegacyDryRunField = "DryRun";

    private static readonly JsonSerializerSettings serializerSettings = new()
    {
        Converters = [new StringEnumConverter()],
        Formatting = Formatting.Indented,
    };

    private static readonly JsonSerializer serializer = JsonSerializer.Create(serializerSettings);

    private readonly List<RunRecord> records = [];
    private readonly string directory;
    private readonly string path;

    public RunHistory(string directory)
    {
        this.directory = directory;
        path = Path.Combine(directory, FileName);
        Load();
    }

    public IReadOnlyList<RunRecord> Records => records;

    public int Revision { get; private set; }

    public void Append(RunRecord record)
    {
        records.Add(record);
        Trim();
        Revision++;
        Save();
    }

    public void Clear()
    {
        records.Clear();
        Revision++;
        Save();
    }

    private void Trim()
    {
        if (records.Count > MaxRecords)
        {
            records.RemoveRange(0, records.Count - MaxRecords);
        }
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(path))
            {
                return;
            }

            if (JToken.Parse(File.ReadAllText(path)) is not JObject file || file[nameof(HistoryFile.Records)] is not JArray rows)
            {
                return;
            }

            var dropped = DropDryRuns(rows);
            records.AddRange(rows.ToObject<List<RunRecord>>(serializer) ?? []);
            Trim();
            Revision++;
            if (dropped > 0)
            {
                RunLog.Info($"The run history dropped {dropped} dry run(s), which handed nothing in");
                Save();
            }
        }
        catch (Exception exception)
        {
            RunLog.Warning(exception, "The run history could not be read; starting empty");
        }
    }

    private static int DropDryRuns(JArray rows)
    {
        var dropped = 0;
        for (var index = rows.Count - 1; index >= 0; index--)
        {
            if (rows[index] is JObject row && row.Value<bool?>(LegacyDryRunField) == true)
            {
                rows.RemoveAt(index);
                dropped++;
            }
        }

        return dropped;
    }

    private void Save()
    {
        var temporary = path + TemporarySuffix;
        try
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(temporary, JsonConvert.SerializeObject(new HistoryFile(FileVersion, records), serializerSettings));
            File.Move(temporary, path, overwrite: true);
        }
        catch (Exception exception)
        {
            RunLog.Warning(exception, "The run history could not be saved");
        }
    }

    private sealed record HistoryFile(int Version, List<RunRecord> Records);
}
