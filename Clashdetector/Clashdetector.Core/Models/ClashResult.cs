namespace Clashdetector.Core.Models;

public sealed class ClashResult
{
    public Guid ConfigId { get; set; }

    public string ConfigName { get; set; } = string.Empty;

    public ModelRef ModelA { get; set; } = ModelRef.Host();

    public ModelRef ModelB { get; set; } = ModelRef.Host();

    public int ElementAId { get; set; }

    public int ElementBId { get; set; }

    public string CategoryA { get; set; } = string.Empty;

    public string CategoryB { get; set; } = string.Empty;

    public SeverityLevel Severity { get; set; } = SeverityLevel.Low;

    public double PenetrationDepth { get; set; }

    public ClashLocation Location { get; set; } = new();

    public string Suggestion { get; set; } = string.Empty;

    public DateTimeOffset RunTimestampUtc { get; set; } = DateTimeOffset.UtcNow;

    public string DedupKey { get; set; } = string.Empty;
}
