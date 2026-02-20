using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Clashdetector.Core.Contracts;
using Clashdetector.Core.Models;
using Clashdetector.Core.Services;
using Clashdetector.UI;

namespace Clashdetector.Revit;

public sealed class RevitRequestHandler : IExternalEventHandler
{
    private readonly ClashAddinContext _context;
    private readonly ClashPaneViewModel _viewModel;
    private readonly IClashDetectionService _detectionService;
    private readonly RevitModelResolver _modelResolver;
    private readonly DiagnosticLogger _logger;
    private readonly Queue<RevitRequest> _queue = new();
    private readonly object _sync = new();

    public RevitRequestHandler(
        ClashAddinContext context,
        ClashPaneViewModel viewModel,
        IClashDetectionService detectionService,
        RevitModelResolver modelResolver,
        DiagnosticLogger logger)
    {
        _context = context;
        _viewModel = viewModel;
        _detectionService = detectionService;
        _modelResolver = modelResolver;
        _logger = logger;
    }

    public string GetName()
    {
        return "Clash Detector Request Handler";
    }

    public void Enqueue(RevitRequest request)
    {
        lock (_sync)
        {
            _queue.Enqueue(request);
        }
    }

    public void Execute(UIApplication app)
    {
        _context.SetCurrentUIApplication(app);

        while (TryDequeue(out var request))
        {
            try
            {
                ProcessRequest(app, request!);
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to process request '{request!.Type}'.", ex);
                _viewModel.SetStatus($"Request failed: {request.Type}");
            }
        }
    }

    private bool TryDequeue(out RevitRequest? request)
    {
        lock (_sync)
        {
            if (_queue.Count == 0)
            {
                request = null;
                return false;
            }

            request = _queue.Dequeue();
            return true;
        }
    }

    private void ProcessRequest(UIApplication app, RevitRequest request)
    {
        switch (request.Type)
        {
            case RevitRequestType.RunManual:
                RunDetection(isAutoRun: false, isIncremental: false, changedByModel: EmptyChangedMap());
                break;
            case RevitRequestType.RunAuto:
                RunDetection(isAutoRun: true, isIncremental: true, changedByModel: request.ChangedElementIdsByModel);
                break;
            case RevitRequestType.FocusSelected:
                FocusSelected(app);
                break;
            case RevitRequestType.IsolateSelected:
                IsolateSelected(app);
                break;
            case RevitRequestType.RefreshCatalogs:
                RefreshCatalogs();
                break;
            default:
                break;
        }
    }

    private void RefreshCatalogs()
    {
        _viewModel.UpdateAvailableModels(_modelResolver.GetAvailableModels());
        _viewModel.UpdateAvailableCategories(_modelResolver.GetAvailableModelCategoryNames());
        _viewModel.SetStatus("Refreshed model and category catalogs.");
    }

    private void RunDetection(
        bool isAutoRun,
        bool isIncremental,
        IReadOnlyDictionary<string, IReadOnlyCollection<int>> changedByModel)
    {
        var detectionRequest = _viewModel.BuildDetectionRequest(
            isIncremental: isIncremental,
            forAutoMode: isAutoRun,
            changedByModel: changedByModel);

        if (detectionRequest.ActiveConfigs.Count == 0)
        {
            _viewModel.SetStatus(isAutoRun
                ? "No active auto-run configs."
                : "No active manual-run configs.");
            return;
        }

        var summary = _detectionService.Run(detectionRequest);
        _viewModel.ApplyRunSummary(summary, isAutoRun);
        _viewModel.SaveSettings();
    }

    private void FocusSelected(UIApplication app)
    {
        var uidoc = app.ActiveUIDocument;
        var selected = _viewModel.SelectedResult;
        if (uidoc is null || selected is null)
        {
            _viewModel.SetStatus("Select a clash result first.");
            return;
        }

        var elementIds = ResolveFocusableElementIds(uidoc.Document, selected);
        if (elementIds.Count == 0)
        {
            _viewModel.SetStatus("Unable to resolve elements for focus.");
            return;
        }

        uidoc.Selection.SetElementIds(elementIds);
        uidoc.ShowElements(elementIds);
        _viewModel.SetStatus("Focused selected clash.");
    }

    private void IsolateSelected(UIApplication app)
    {
        var uidoc = app.ActiveUIDocument;
        var selected = _viewModel.SelectedResult;
        if (uidoc is null || selected is null)
        {
            _viewModel.SetStatus("Select a clash result first.");
            return;
        }

        var elementIds = ResolveFocusableElementIds(uidoc.Document, selected);
        if (elementIds.Count == 0)
        {
            _viewModel.SetStatus("Unable to resolve elements for isolate.");
            return;
        }

        using var tx = new Transaction(uidoc.Document, "Isolate Clash Elements");
        tx.Start();
        uidoc.ActiveView.IsolateElementsTemporary(elementIds);
        tx.Commit();
        _viewModel.SetStatus("Temporarily isolated selected clash.");
    }

    private static ICollection<ElementId> ResolveFocusableElementIds(Document hostDoc, ClashResult result)
    {
        var ids = new List<ElementId>();
        TryAddElement(hostDoc, result.ModelA, result.ElementAId, ids);
        TryAddElement(hostDoc, result.ModelB, result.ElementBId, ids);
        return ids;
    }

    private static void TryAddElement(Document hostDoc, ModelRef modelRef, int elementId, ICollection<ElementId> destination)
    {
        if (modelRef.Kind == ModelKind.Host)
        {
            var id = new ElementId(elementId);
            if (hostDoc.GetElement(id) is not null)
            {
                destination.Add(id);
            }

            return;
        }

        if (modelRef.LinkInstanceId is null)
        {
            return;
        }

        var linkId = new ElementId(modelRef.LinkInstanceId.Value);
        if (hostDoc.GetElement(linkId) is not null)
        {
            destination.Add(linkId);
        }
    }

    private static IReadOnlyDictionary<string, IReadOnlyCollection<int>> EmptyChangedMap()
    {
        return new Dictionary<string, IReadOnlyCollection<int>>(StringComparer.OrdinalIgnoreCase);
    }
}
