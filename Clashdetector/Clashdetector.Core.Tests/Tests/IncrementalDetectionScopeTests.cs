using Clashdetector.Core.Models;
using Clashdetector.Core.Utilities;

namespace Clashdetector.Core.Tests.Tests;

public sealed class IncrementalDetectionScopeTests
{
    [Fact]
    public void ShouldEvaluatePair_OnlyReturnsTrueWhenAtLeastOneElementChanged()
    {
        var config = new ClashConfig
        {
            ModelA = ModelRef.Host("Host"),
            ModelB = ModelRef.Link(5, "Link"),
        };

        var changed = new Dictionary<string, IReadOnlyCollection<int>>(StringComparer.OrdinalIgnoreCase)
        {
            [config.ModelA.StableKey] = new[] { 100 },
            [config.ModelB.StableKey] = new[] { 200 },
        };

        Assert.True(IncrementalDetectionScope.ShouldEvaluatePair(config, 100, 333, changed));
        Assert.True(IncrementalDetectionScope.ShouldEvaluatePair(config, 333, 200, changed));
        Assert.False(IncrementalDetectionScope.ShouldEvaluatePair(config, 333, 444, changed));
    }
}
