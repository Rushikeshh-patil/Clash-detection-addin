using Clashdetector.Core.Contracts;

namespace Clashdetector.Core.Services;

public sealed class RuleBasedClashSuggestionProvider : IClashSuggestionProvider
{
    private static readonly IReadOnlyDictionary<string, string> Suggestions =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Ducts|Pipes"] = "Consider rerouting duct or offsetting pipe elevation near the conflict.",
            ["Cable Trays|Ducts"] = "Shift tray path or resize duct offset to restore required clearances.",
            ["Pipes|Structural Framing"] = "Check sleeve/opening options and coordinate with structural team.",
            ["Conduits|Pipes"] = "Review route stacking and apply vertical staggering to reduce overlap.",
            ["Walls|Pipes"] = "Add or resize wall opening and verify firestopping requirements.",
            ["Floors|Pipes"] = "Coordinate penetration locations with sleeves before detailing.",
        };

    public string GetSuggestion(string categoryA, string categoryB)
    {
        if (string.IsNullOrWhiteSpace(categoryA) || string.IsNullOrWhiteSpace(categoryB))
        {
            return "Review geometry and coordinate an offset, reroute, or opening with the affected teams.";
        }

        var key = BuildPairKey(categoryA, categoryB);
        if (Suggestions.TryGetValue(key, out var suggestion))
        {
            return suggestion;
        }

        return "Review geometry and coordinate an offset, reroute, or opening with the affected teams.";
    }

    private static string BuildPairKey(string categoryA, string categoryB)
    {
        var first = categoryA.Trim();
        var second = categoryB.Trim();
        return string.Compare(first, second, StringComparison.OrdinalIgnoreCase) <= 0
            ? $"{first}|{second}"
            : $"{second}|{first}";
    }
}
