namespace Clashdetector.Revit;

public sealed class RevitRequest
{
    public RevitRequestType Type { get; set; }

    public IReadOnlyDictionary<string, IReadOnlyCollection<int>> ChangedElementIdsByModel { get; set; }
        = new Dictionary<string, IReadOnlyCollection<int>>(StringComparer.OrdinalIgnoreCase);
}
