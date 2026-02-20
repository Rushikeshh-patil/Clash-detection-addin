namespace Clashdetector.Core.Models;

public sealed class SeverityThresholds
{
    public double MediumMin { get; set; } = 0.1;

    public double HighMin { get; set; } = 0.5;

    public SeverityThresholds Clone()
    {
        return new SeverityThresholds
        {
            MediumMin = MediumMin,
            HighMin = HighMin,
        };
    }
}
