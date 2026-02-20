using Clashdetector.Core.Models;

namespace Clashdetector.Core.Utilities;

public static class IncrementalDetectionScope
{
    public static bool ShouldProcessConfig(
        ClashConfig config,
        IReadOnlyDictionary<string, IReadOnlyCollection<int>> changedByModel)
    {
        ArgumentNullException.ThrowIfNull(config);

        if (changedByModel.Count == 0)
        {
            return true;
        }

        return changedByModel.ContainsKey(config.ModelA.StableKey)
            || changedByModel.ContainsKey(config.ModelB.StableKey);
    }

    public static bool ShouldEvaluatePair(
        ClashConfig config,
        int elementAId,
        int elementBId,
        IReadOnlyDictionary<string, IReadOnlyCollection<int>> changedByModel)
    {
        if (changedByModel.Count == 0)
        {
            return true;
        }

        var hasA = changedByModel.TryGetValue(config.ModelA.StableKey, out var changedA);
        var hasB = changedByModel.TryGetValue(config.ModelB.StableKey, out var changedB);

        if (!hasA && !hasB)
        {
            return false;
        }

        var aChanged = hasA && changedA!.Contains(elementAId);
        var bChanged = hasB && changedB!.Contains(elementBId);

        return aChanged || bChanged;
    }
}
