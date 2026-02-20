using System.Text.Json.Serialization;

namespace Clashdetector.Core.Models;

public sealed class ModelRef : IEquatable<ModelRef>
{
    public ModelKind Kind { get; set; } = ModelKind.Host;

    public int? LinkInstanceId { get; set; }

    public string DisplayName { get; set; } = "Host Model";

    [JsonIgnore]
    public string StableKey => Kind == ModelKind.Host
        ? "host"
        : $"link:{LinkInstanceId?.ToString() ?? "unknown"}";

    public static ModelRef Host(string displayName = "Host Model")
    {
        return new ModelRef
        {
            Kind = ModelKind.Host,
            DisplayName = displayName,
            LinkInstanceId = null,
        };
    }

    public static ModelRef Link(int linkInstanceId, string displayName)
    {
        return new ModelRef
        {
            Kind = ModelKind.Link,
            LinkInstanceId = linkInstanceId,
            DisplayName = displayName,
        };
    }

    public bool Equals(ModelRef? other)
    {
        if (other is null)
        {
            return false;
        }

        return Kind == other.Kind && LinkInstanceId == other.LinkInstanceId;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as ModelRef);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine((int)Kind, LinkInstanceId ?? -1);
    }

    public override string ToString()
    {
        return DisplayName;
    }
}
