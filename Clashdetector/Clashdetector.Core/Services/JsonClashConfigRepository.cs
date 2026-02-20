using System.Text.Json;
using Clashdetector.Core.Contracts;
using Clashdetector.Core.Models;

namespace Clashdetector.Core.Services;

public sealed class JsonClashConfigRepository : IClashConfigRepository
{
    private readonly string _settingsFilePath;
    private readonly DiagnosticLogger _logger;
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public JsonClashConfigRepository(string settingsFilePath, DiagnosticLogger logger)
    {
        _settingsFilePath = settingsFilePath;
        _logger = logger;
    }

    public ClashSettingsState Load()
    {
        try
        {
            if (!File.Exists(_settingsFilePath))
            {
                var defaults = ClashSettingsState.CreateDefault();
                Save(defaults);
                return defaults;
            }

            var json = File.ReadAllText(_settingsFilePath);
            var state = JsonSerializer.Deserialize<ClashSettingsState>(json, _serializerOptions);
            var normalized = Normalize(state);
            _logger.Info($"Loaded clash settings from '{_settingsFilePath}'.");
            return normalized;
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to load settings. Falling back to defaults.", ex);
            var defaults = ClashSettingsState.CreateDefault();
            Save(defaults);
            return defaults;
        }
    }

    public void Save(ClashSettingsState state)
    {
        var normalized = Normalize(state);
        var dirPath = Path.GetDirectoryName(_settingsFilePath);
        if (!string.IsNullOrWhiteSpace(dirPath))
        {
            Directory.CreateDirectory(dirPath);
        }

        var json = JsonSerializer.Serialize(normalized, _serializerOptions);
        File.WriteAllText(_settingsFilePath, json);
        _logger.Info($"Saved clash settings to '{_settingsFilePath}'.");
    }

    private static ClashSettingsState Normalize(ClashSettingsState? state)
    {
        if (state is null)
        {
            return ClashSettingsState.CreateDefault();
        }

        state.Configs ??= new List<ClashConfig>();
        if (state.Configs.Count == 0)
        {
            state.Configs.Add(ClashConfig.CreateDefault());
        }

        foreach (var config in state.Configs)
        {
            config.Name = string.IsNullOrWhiteSpace(config.Name) ? "Unnamed Config" : config.Name.Trim();
            config.ModelA ??= ModelRef.Host();
            config.ModelB ??= ModelRef.Host();
            config.CategoryPairs ??= new List<CategoryPairRule>();
            config.SeverityThresholds ??= new SeverityThresholds();
            config.ColorSettings ??= new ClashColorSettings();
        }

        if (state.AutoDebounceMilliseconds < 250)
        {
            state.AutoDebounceMilliseconds = 250;
        }

        return state;
    }
}
