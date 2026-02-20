using Autodesk.Revit.UI;
using Clashdetector.Core.Contracts;
using Clashdetector.Core.Services;
using Clashdetector.Infrastructure;
using Clashdetector.Revit;
using Clashdetector.UI;

namespace Clashdetector;

public sealed class ClashAddinContext : IDisposable
{
    private static readonly Lazy<ClashAddinContext> LazyInstance = new(() => new ClashAddinContext());

    private bool _initialized;
    private UIApplication? _currentUiApplication;
    private RevitRequestHandler? _requestHandler;
    private ExternalEvent? _externalEvent;

    private ClashAddinContext()
    {
        AutoDetectionCoordinator = new DebouncedAutoDetectionCoordinator();
    }

    public static ClashAddinContext Instance => LazyInstance.Value;

    public DockablePaneId DockablePaneId { get; } = new DockablePaneId(new Guid(RevitIds.DockablePaneGuid));

    public IAutoDetectionCoordinator AutoDetectionCoordinator { get; private set; }

    public DiagnosticLogger Logger { get; private set; } = new DiagnosticLogger(AppPaths.LogsDirectory);

    public ClashPaneViewModel ViewModel { get; private set; } = null!;

    public RevitModelResolver ModelResolver { get; private set; } = null!;

    public IClashDetectionService DetectionService { get; private set; } = null!;

    public void Initialize(UIControlledApplication application)
    {
        if (_initialized)
        {
            return;
        }

        Logger = new DiagnosticLogger(AppPaths.LogsDirectory);
        AutoDetectionCoordinator = new DebouncedAutoDetectionCoordinator();
        var repository = new JsonClashConfigRepository(AppPaths.SettingsFilePath, Logger);
        var exporter = new CsvClashExporter();
        var suggestionProvider = new RuleBasedClashSuggestionProvider();

        ModelResolver = new RevitModelResolver(() => _currentUiApplication?.ActiveUIDocument?.Document);
        DetectionService = new RevitClashDetectionService(
            ModelResolver,
            suggestionProvider,
            Logger,
            () => _currentUiApplication?.ActiveUIDocument?.Document);

        ViewModel = new ClashPaneViewModel(
            repository,
            exporter,
            AutoDetectionCoordinator,
            Logger,
            ModelResolver);

        _requestHandler = new RevitRequestHandler(
            this,
            ViewModel,
            DetectionService,
            ModelResolver,
            Logger);
        _externalEvent = ExternalEvent.Create(_requestHandler);

        var paneControl = new ClashPaneControl(ViewModel, this);
        var provider = new ClashDockablePaneProvider(paneControl);
        application.RegisterDockablePane(DockablePaneId, "Clash Detector", provider);

        _initialized = true;
        Logger.Info("Clash add-in context initialized.");
    }

    public void SetCurrentUIApplication(UIApplication application)
    {
        _currentUiApplication = application;
    }

    public UIApplication? GetCurrentUIApplication()
    {
        return _currentUiApplication;
    }

    public void QueueRequest(RevitRequest request)
    {
        if (!_initialized || _requestHandler is null || _externalEvent is null)
        {
            return;
        }

        _requestHandler.Enqueue(request);
        try
        {
            _externalEvent.Raise();
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to raise external event.", ex);
        }
    }

    public void ShowPane(UIApplication uiApplication)
    {
        _currentUiApplication = uiApplication;
        var pane = uiApplication.GetDockablePane(DockablePaneId);
        pane.Show();
    }

    public void Dispose()
    {
        if (!_initialized)
        {
            return;
        }

        ViewModel.SaveSettings();
        AutoDetectionCoordinator.ClearPending();
        Logger.Info("Clash add-in context disposed.");

        _initialized = false;
        _currentUiApplication = null;
        _requestHandler = null;
        _externalEvent = null;
    }
}
