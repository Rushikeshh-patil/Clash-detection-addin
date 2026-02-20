using Clashdetector.Core.Models;

namespace Clashdetector.Core.Utilities;

public static class SeverityClassifier
{
    public static SeverityLevel Classify(double penetrationDepth, SeverityThresholds thresholds)
    {
        ArgumentNullException.ThrowIfNull(thresholds);

        if (penetrationDepth >= thresholds.HighMin)
        {
            return SeverityLevel.High;
        }

        if (penetrationDepth >= thresholds.MediumMin)
        {
            return SeverityLevel.Medium;
        }

        return SeverityLevel.Low;
    }
}
