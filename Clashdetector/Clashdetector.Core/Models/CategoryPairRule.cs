namespace Clashdetector.Core.Models;

public sealed class CategoryPairRule
{
    public string CategoryA { get; set; } = string.Empty;

    public string CategoryB { get; set; } = string.Empty;

    public bool Enabled { get; set; } = true;

    public CategoryPairRule Clone()
    {
        return new CategoryPairRule
        {
            CategoryA = CategoryA,
            CategoryB = CategoryB,
            Enabled = Enabled,
        };
    }
}
