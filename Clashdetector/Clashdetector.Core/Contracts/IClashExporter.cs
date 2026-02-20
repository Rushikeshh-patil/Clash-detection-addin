using Clashdetector.Core.Models;

namespace Clashdetector.Core.Contracts;

public interface IClashExporter
{
    void ExportCsv(
        string filePath,
        IReadOnlyList<ClashResult> results,
        IReadOnlyDictionary<Guid, DetectionMetrics> metricsByConfig);
}
