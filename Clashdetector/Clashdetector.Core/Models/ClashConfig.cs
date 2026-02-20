namespace Clashdetector.Core.Models;

public sealed class ClashConfig
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = "New Clash Config";

    public bool IsActive { get; set; } = true;

    public ModelRef ModelA { get; set; } = ModelRef.Host();

    public ModelRef ModelB { get; set; } = ModelRef.Host();

    public List<CategoryPairRule> CategoryPairs { get; set; } = new();

    public SeverityThresholds SeverityThresholds { get; set; } = new();

    public ClashColorSettings ColorSettings { get; set; } = new();

    public bool RunInManualMode { get; set; } = true;

    public bool RunInAutoMode { get; set; } = true;

    public ClashConfig Clone(string? newName = null)
    {
        var clone = new ClashConfig
        {
            Id = Guid.NewGuid(),
            Name = newName ?? $"{Name} Copy",
            IsActive = IsActive,
            ModelA = new ModelRef
            {
                Kind = ModelA.Kind,
                LinkInstanceId = ModelA.LinkInstanceId,
                DisplayName = ModelA.DisplayName,
            },
            ModelB = new ModelRef
            {
                Kind = ModelB.Kind,
                LinkInstanceId = ModelB.LinkInstanceId,
                DisplayName = ModelB.DisplayName,
            },
            CategoryPairs = CategoryPairs.Select(x => x.Clone()).ToList(),
            SeverityThresholds = SeverityThresholds.Clone(),
            ColorSettings = ColorSettings.Clone(),
            RunInManualMode = RunInManualMode,
            RunInAutoMode = RunInAutoMode,
        };

        return clone;
    }

    public static ClashConfig CreateDefault()
    {
        return new ClashConfig
        {
            Name = "Default Config",
            IsActive = true,
            ModelA = ModelRef.Host(),
            ModelB = ModelRef.Host(),
            CategoryPairs = new List<CategoryPairRule>
            {
                new()
                {
                    CategoryA = "Ducts",
                    CategoryB = "Pipes",
                    Enabled = true,
                },
            },
        };
    }
}
