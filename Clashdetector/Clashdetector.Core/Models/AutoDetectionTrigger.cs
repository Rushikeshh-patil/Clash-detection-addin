namespace Clashdetector.Core.Models;

public sealed class AutoDetectionTrigger
{
    public DateTimeOffset TriggeredAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public IReadOnlyDictionary<string, IReadOnlyCollection<int>> ChangedElementIdsByModel { get; set; }
        = new Dictionary<string, IReadOnlyCollection<int>>(StringComparer.OrdinalIgnoreCase);
}
