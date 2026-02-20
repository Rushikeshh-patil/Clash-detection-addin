using System.Globalization;
using System.Text;
using Clashdetector.Core.Contracts;
using Clashdetector.Core.Models;

namespace Clashdetector.Core.Services;

public sealed class CsvClashExporter : IClashExporter
{
    public void ExportCsv(
        string filePath,
        IReadOnlyList<ClashResult> results,
        IReadOnlyDictionary<Guid, DetectionMetrics> metricsByConfig)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var sb = new StringBuilder();
        sb.AppendLine(
            "RunTimestamp,ConfigName,ModelA,ModelB,ElementAId,ElementBId,CategoryA,CategoryB,Severity,PenetrationDepth,LocationX,LocationY,LocationZ,Suggestion,RunDurationMs,ElementsScanned,ClashesFound");

        foreach (var result in results)
        {
            metricsByConfig.TryGetValue(result.ConfigId, out var metrics);
            var row = new[]
            {
                Escape(result.RunTimestampUtc.ToString("O", CultureInfo.InvariantCulture)),
                Escape(result.ConfigName),
                Escape(result.ModelA.DisplayName),
                Escape(result.ModelB.DisplayName),
                Escape(result.ElementAId.ToString(CultureInfo.InvariantCulture)),
                Escape(result.ElementBId.ToString(CultureInfo.InvariantCulture)),
                Escape(result.CategoryA),
                Escape(result.CategoryB),
                Escape(result.Severity.ToString()),
                Escape(result.PenetrationDepth.ToString("0.######", CultureInfo.InvariantCulture)),
                Escape(result.Location.X.ToString("0.######", CultureInfo.InvariantCulture)),
                Escape(result.Location.Y.ToString("0.######", CultureInfo.InvariantCulture)),
                Escape(result.Location.Z.ToString("0.######", CultureInfo.InvariantCulture)),
                Escape(result.Suggestion),
                Escape((metrics?.DurationMs ?? 0).ToString(CultureInfo.InvariantCulture)),
                Escape((metrics?.ElementsScanned ?? 0).ToString(CultureInfo.InvariantCulture)),
                Escape((metrics?.ConfirmedClashes ?? 0).ToString(CultureInfo.InvariantCulture)),
            };

            sb.AppendLine(string.Join(",", row));
        }

        File.WriteAllText(filePath, sb.ToString(), new UTF8Encoding(false));
    }

    private static string Escape(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
        {
            return "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
        }

        return value;
    }
}
