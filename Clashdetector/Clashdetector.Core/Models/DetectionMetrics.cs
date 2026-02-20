namespace Clashdetector.Core.Models;

public sealed class DetectionMetrics
{
    public DateTimeOffset TimestampUtc { get; set; } = DateTimeOffset.UtcNow;

    public long DurationMs { get; set; }

    public int ElementsScanned { get; set; }

    public int CandidatePairs { get; set; }

    public int ConfirmedClashes { get; set; }
}
