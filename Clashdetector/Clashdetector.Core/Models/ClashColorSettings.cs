namespace Clashdetector.Core.Models;

public sealed class ClashColorSettings
{
    public string LowColorHex { get; set; } = "#E6B94A";

    public string MediumColorHex { get; set; } = "#F97316";

    public string HighColorHex { get; set; } = "#DC2626";

    public ClashColorSettings Clone()
    {
        return new ClashColorSettings
        {
            LowColorHex = LowColorHex,
            MediumColorHex = MediumColorHex,
            HighColorHex = HighColorHex,
        };
    }
}
