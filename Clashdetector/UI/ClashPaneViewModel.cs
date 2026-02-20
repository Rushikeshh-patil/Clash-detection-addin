using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Clashdetector.Core.Contracts;
using Clashdetector.Core.Models;
using Clashdetector.Core.Services;
using Clashdetector.Revit;

namespace Clashdetector.UI;

public sealed class ClashPaneViewModel : INotifyPropertyChanged
{
    private readonly IClashConfigRepository _configRepository;
    private readonly IClashExporter _exporter;
    private readonly IAutoDetectionCoordinator _autoDetectionCoordinator;
    private readonly DiagnosticLogger _logger;
    private readonly RevitModelResolver _modelResolver;

    private ClashConfig? _selectedConfig;
    private CategoryPairRule? _selectedCategoryPair;
    private ClashResult? _selectedResult;
    private bool _autoModeEnabled;
    private int _debounceMilliseconds = 1500;
    private string _statusMessage = "Ready";
    private string _metricsSummary = "No runs yet.";
    private Dictionary<Guid, DetectionMetrics> _lastMetricsByConfig = new();

    public ClashPaneViewModel(
        IClashConfigRepository configRepository,
        IClashExporter exporter,
        IAutoDetectionCoordinator autoDetectionCoordinator,
        DiagnosticLogger logger,
        RevitModelResolver modelResolver)
    {
        _configRepository = configRepository;
        _exporter = exporter;
        _autoDetectionCoordinator = autoDetectionCoordinator;
        _logger = logger;
        _modelResolver = modelResolver;

        LoadSettings();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<ClashConfig> Configs { get; } = new();

    public ObservableCollection<ModelRef> AvailableModels { get; } = new();

    public ObservableCollection<string> AvailableCategories { get; } = new();

    public ObservableCollection<ClashResult> Results { get; } = new();

    public ClashConfig? SelectedConfig
    {
        get => _selectedConfig;
        set
        {
            if (ReferenceEquals(_selectedConfig, value))
            {
                return;
            }

            _selectedConfig = value;
            OnPropertyChanged();
        }
    }

    public CategoryPairRule? SelectedCategoryPair
    {
        get => _selectedCategoryPair;
        set
        {
            if (ReferenceEquals(_selectedCategoryPair, value))
            {
                return;
            }

            _selectedCategoryPair = value;
            OnPropertyChanged();
        }
    }

    public ClashResult? SelectedResult
    {
        get => _selectedResult;
        set
        {
            if (ReferenceEquals(_selectedResult, value))
            {
                return;
            }

            _selectedResult = value;
            OnPropertyChanged();
        }
    }

    public bool AutoModeEnabled
    {
        get => _autoModeEnabled;
        set
        {
            if (_autoModeEnabled == value)
            {
                return;
            }

            _autoModeEnabled = value;
            _autoDetectionCoordinator.SetAutoMode(value);
            SaveSettings();
            OnPropertyChanged();
        }
    }

    public int DebounceMilliseconds
    {
        get => _debounceMilliseconds;
        set
        {
            var normalized = value < 250 ? 250 : value;
            if (_debounceMilliseconds == normalized)
            {
                return;
            }

            _debounceMilliseconds = normalized;
            _autoDetectionCoordinator.DebounceMilliseconds = normalized;
            SaveSettings();
            OnPropertyChanged();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (string.Equals(_statusMessage, value, StringComparison.Ordinal))
            {
                return;
            }

            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    public string MetricsSummary
    {
        get => _metricsSummary;
        private set
        {
            if (string.Equals(_metricsSummary, value, StringComparison.Ordinal))
            {
                return;
            }

            _metricsSummary = value;
            OnPropertyChanged();
        }
    }

    public void LoadSettings()
    {
        var state = _configRepository.Load();

        Configs.Clear();
        foreach (var config in state.Configs)
        {
            Configs.Add(config);
        }

        if (Configs.Count == 0)
        {
            Configs.Add(ClashConfig.CreateDefault());
        }

        SelectedConfig = state.LastSelectedConfigId is null
            ? Configs.FirstOrDefault()
            : Configs.FirstOrDefault(x => x.Id == state.LastSelectedConfigId.Value) ?? Configs.FirstOrDefault();

        _debounceMilliseconds = state.AutoDebounceMilliseconds < 250 ? 250 : state.AutoDebounceMilliseconds;
        _autoModeEnabled = state.AutoModeEnabled;
        _autoDetectionCoordinator.DebounceMilliseconds = _debounceMilliseconds;
        _autoDetectionCoordinator.SetAutoMode(_autoModeEnabled);
    }

    public void SaveSettings()
    {
        if (Configs.Count == 0)
        {
            Configs.Add(ClashConfig.CreateDefault());
        }

        var state = new ClashSettingsState
        {
            Configs = Configs.ToList(),
            LastSelectedConfigId = SelectedConfig?.Id,
            AutoModeEnabled = AutoModeEnabled,
            AutoDebounceMilliseconds = DebounceMilliseconds,
        };

        _configRepository.Save(state);
    }

    public void CreateConfig()
    {
        var config = ClashConfig.CreateDefault();
        config.Name = $"Config {Configs.Count + 1}";
        if (AvailableModels.Count > 0)
        {
            config.ModelA = CloneModel(AvailableModels[0]);
            config.ModelB = CloneModel(AvailableModels[0]);
        }

        Configs.Add(config);
        SelectedConfig = config;
        SaveSettings();
        SetStatus($"Created '{config.Name}'.");
    }

    public void DuplicateSelectedConfig()
    {
        if (SelectedConfig is null)
        {
            return;
        }

        var clone = SelectedConfig.Clone($"{SelectedConfig.Name} Copy");
        Configs.Add(clone);
        SelectedConfig = clone;
        SaveSettings();
        SetStatus($"Duplicated '{SelectedConfig.Name}'.");
    }

    public void DeleteSelectedConfig()
    {
        if (SelectedConfig is null)
        {
            return;
        }

        var deletedName = SelectedConfig.Name;
        Configs.Remove(SelectedConfig);
        SelectedConfig = Configs.FirstOrDefault();
        if (Configs.Count == 0)
        {
            var fallback = ClashConfig.CreateDefault();
            Configs.Add(fallback);
            SelectedConfig = fallback;
        }

        SaveSettings();
        SetStatus($"Deleted '{deletedName}'.");
    }

    public void AddCategoryPair(string categoryA, string categoryB)
    {
        if (SelectedConfig is null)
        {
            return;
        }

        var a = categoryA.Trim();
        var b = categoryB.Trim();
        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
        {
            SetStatus("Select both categories before adding a pair.");
            return;
        }

        SelectedConfig.CategoryPairs.Add(new CategoryPairRule
        {
            CategoryA = a,
            CategoryB = b,
            Enabled = true,
        });

        OnPropertyChanged(nameof(SelectedConfig));
        SaveSettings();
        SetStatus($"Added rule: {a} vs {b}");
    }

    public void RemoveSelectedCategoryPair()
    {
        if (SelectedConfig is null || SelectedCategoryPair is null)
        {
            return;
        }

        SelectedConfig.CategoryPairs.Remove(SelectedCategoryPair);
        SelectedCategoryPair = null;
        OnPropertyChanged(nameof(SelectedConfig));
        SaveSettings();
        SetStatus("Removed category pair rule.");
    }

    public void UpdateAvailableModels(IReadOnlyList<ModelRef> models)
    {
        AvailableModels.Clear();
        foreach (var model in models)
        {
            AvailableModels.Add(CloneModel(model));
        }

        foreach (var config in Configs)
        {
            config.ModelA = ResolveModelOrDefault(config.ModelA);
            config.ModelB = ResolveModelOrDefault(config.ModelB);
        }

        OnPropertyChanged(nameof(AvailableModels));
        OnPropertyChanged(nameof(SelectedConfig));
        SaveSettings();
    }

    public void UpdateAvailableCategories(IReadOnlyList<string> categories)
    {
        AvailableCategories.Clear();
        foreach (var category in categories)
        {
            AvailableCategories.Add(category);
        }

        OnPropertyChanged(nameof(AvailableCategories));
    }

    public ClashDetectionRequest BuildDetectionRequest(
        bool isIncremental,
        bool forAutoMode,
        IReadOnlyDictionary<string, IReadOnlyCollection<int>> changedByModel)
    {
        var active = Configs
            .Where(x => x.IsActive && (forAutoMode ? x.RunInAutoMode : x.RunInManualMode))
            .ToList();

        return new ClashDetectionRequest
        {
            ActiveConfigs = active,
            IsIncremental = isIncremental,
            ChangedElementIdsByModel = changedByModel,
        };
    }

    public void ApplyRunSummary(DetectionRunSummary summary, bool isAutoRun)
    {
        Results.Clear();
        foreach (var result in summary.Results.OrderByDescending(x => x.Severity).ThenBy(x => x.ConfigName))
        {
            Results.Add(result);
        }

        _lastMetricsByConfig = summary.MetricsByConfig.ToDictionary(
            x => x.Key,
            x => x.Value);

        var mode = isAutoRun ? "Auto" : "Manual";
        MetricsSummary =
            $"{mode} run: {summary.TotalMetrics.ConfirmedClashes} clashes, " +
            $"{summary.TotalMetrics.ElementsScanned} elements, " +
            $"{summary.TotalMetrics.DurationMs} ms.";

        StatusMessage = $"Completed {mode.ToLowerInvariant()} detection for {summary.MetricsByConfig.Count} config(s).";
    }

    public void ClearResults()
    {
        Results.Clear();
        _lastMetricsByConfig.Clear();
        SelectedResult = null;
        MetricsSummary = "No runs yet.";
        StatusMessage = "Cleared results.";
    }

    public void ExportResults(string filePath)
    {
        var metrics = _lastMetricsByConfig.Count > 0
            ? _lastMetricsByConfig
            : Results
                .GroupBy(x => x.ConfigId)
                .ToDictionary(
                    x => x.Key,
                    x => new DetectionMetrics
                    {
                        TimestampUtc = DateTimeOffset.UtcNow,
                        ConfirmedClashes = x.Count(),
                    });

        _exporter.ExportCsv(filePath, Results.ToList(), metrics);
        SetStatus($"Exported {Results.Count} clash rows to '{filePath}'.");
    }

    public void RefreshCatalogData()
    {
        UpdateAvailableModels(_modelResolver.GetAvailableModels());
        UpdateAvailableCategories(_modelResolver.GetAvailableModelCategoryNames());
    }

    public void SetStatus(string status)
    {
        StatusMessage = status;
        _logger.Info(status);
    }

    private ModelRef ResolveModelOrDefault(ModelRef existing)
    {
        var match = AvailableModels.FirstOrDefault(x => x.Equals(existing));
        if (match is not null)
        {
            return CloneModel(match);
        }

        if (AvailableModels.Count > 0)
        {
            return CloneModel(AvailableModels[0]);
        }

        return ModelRef.Host();
    }

    private static ModelRef CloneModel(ModelRef model)
    {
        return new ModelRef
        {
            Kind = model.Kind,
            LinkInstanceId = model.LinkInstanceId,
            DisplayName = model.DisplayName,
        };
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
