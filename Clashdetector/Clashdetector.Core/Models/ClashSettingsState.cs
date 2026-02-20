namespace Clashdetector.Core.Models;

public sealed class ClashSettingsState
{
    public List<ClashConfig> Configs { get; set; } = new();

    public Guid? LastSelectedConfigId { get; set; }

    public bool AutoModeEnabled { get; set; }

    public int AutoDebounceMilliseconds { get; set; } = 1500;

    public static ClashSettingsState CreateDefault()
    {
        var config = ClashConfig.CreateDefault();
        return new ClashSettingsState
        {
            Configs = new List<ClashConfig> { config },
            LastSelectedConfigId = config.Id,
            AutoModeEnabled = false,
            AutoDebounceMilliseconds = 1500,
        };
    }
}
