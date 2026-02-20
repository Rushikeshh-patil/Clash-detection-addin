using Autodesk.Revit.DB;
using Clashdetector.Core.Models;

namespace Clashdetector.Revit;

public sealed class ResolvedModel
{
    public required Document Document { get; init; }

    public required Transform TransformToHost { get; init; }

    public required string StableKey { get; init; }

    public required string DisplayName { get; init; }

    public required ModelRef ModelRef { get; init; }

    public ElementId? LinkInstanceId { get; init; }
}
