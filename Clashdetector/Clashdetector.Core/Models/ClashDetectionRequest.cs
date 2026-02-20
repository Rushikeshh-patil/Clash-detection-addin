namespace Clashdetector.Core.Models;

public sealed class ClashDetectionRequest
{
    public IReadOnlyList<ClashConfig> ActiveConfigs { get; set; } = Array.Empty<ClashConfig>();

    public bool IsIncremental { get; set; }

    public IReadOnlyDictionary<string, IReadOnlyCollection<int>> ChangedElementIdsByModel { get; set; }
        = new Dictionary<string, IReadOnlyCollection<int>>(StringComparer.OrdinalIgnoreCase);
}
