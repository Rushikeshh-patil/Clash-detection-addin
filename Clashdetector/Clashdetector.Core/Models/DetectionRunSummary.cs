namespace Clashdetector.Core.Models;

public sealed class DetectionRunSummary
{
    public List<ClashResult> Results { get; set; } = new();

    public Dictionary<Guid, DetectionMetrics> MetricsByConfig { get; set; } = new();

    public DetectionMetrics TotalMetrics { get; set; } = new();

    public static DetectionRunSummary Empty()
    {
        return new DetectionRunSummary();
    }
}
