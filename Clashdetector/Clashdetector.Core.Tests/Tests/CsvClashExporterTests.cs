using Clashdetector.Core.Models;
using Clashdetector.Core.Services;

namespace Clashdetector.Core.Tests.Tests;

public sealed class CsvClashExporterTests
{
    [Fact]
    public void ExportCsv_WritesHeaderAndDataRows()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), "ClashdetectorTests", Guid.NewGuid().ToString("N"), "output.csv");
        var exporter = new CsvClashExporter();
        var configId = Guid.NewGuid();
        var results = new List<ClashResult>
        {
            new()
            {
                ConfigId = configId,
                ConfigName = "Config A",
                ModelA = ModelRef.Host("Host"),
                ModelB = ModelRef.Link(8, "MEP Link"),
                ElementAId = 10,
                ElementBId = 22,
                CategoryA = "Ducts",
                CategoryB = "Pipes",
                PenetrationDepth = 0.42,
                Suggestion = "Shift duct",
                Location = new ClashLocation { X = 1, Y = 2, Z = 3 },
            },
        };
        var metrics = new Dictionary<Guid, DetectionMetrics>
        {
            [configId] = new DetectionMetrics
            {
                DurationMs = 120,
                ElementsScanned = 40,
                ConfirmedClashes = 1,
            },
        };

        exporter.ExportCsv(tempFile, results, metrics);
        var csv = File.ReadAllText(tempFile);

        Assert.Contains("RunTimestamp,ConfigName,ModelA,ModelB", csv, StringComparison.Ordinal);
        Assert.Contains("Config A", csv, StringComparison.Ordinal);
        Assert.Contains("120", csv, StringComparison.Ordinal);
    }
}
