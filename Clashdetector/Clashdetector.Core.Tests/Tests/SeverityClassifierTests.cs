using Clashdetector.Core.Models;
using Clashdetector.Core.Utilities;

namespace Clashdetector.Core.Tests.Tests;

public sealed class SeverityClassifierTests
{
    [Theory]
    [InlineData(0.05, SeverityLevel.Low)]
    [InlineData(0.3, SeverityLevel.Medium)]
    [InlineData(0.9, SeverityLevel.High)]
    public void Classify_MapsDepthToTier(double depth, SeverityLevel expected)
    {
        var thresholds = new SeverityThresholds
        {
            MediumMin = 0.2,
            HighMin = 0.7,
        };

        var actual = SeverityClassifier.Classify(depth, thresholds);

        Assert.Equal(expected, actual);
    }
}
