using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using Clashdetector.Infrastructure;
using Clashdetector.Revit;

namespace Clashdetector;

[Transaction(TransactionMode.Manual)]
public sealed class App : IExternalApplication
{
    public Result OnStartup(UIControlledApplication application)
    {
        try
        {
            ClashAddinContext.Instance.Initialize(application);
            RegisterRibbon(application);
            application.ControlledApplication.DocumentChanged += OnDocumentChanged;
            application.Idling += OnIdling;
            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            TaskDialog.Show("Clash Detector", $"Failed to start add-in.{Environment.NewLine}{ex.Message}");
            return Result.Failed;
        }
    }

    public Result OnShutdown(UIControlledApplication application)
    {
        application.ControlledApplication.DocumentChanged -= OnDocumentChanged;
        application.Idling -= OnIdling;
        ClashAddinContext.Instance.Dispose();
        return Result.Succeeded;
    }

    private static void RegisterRibbon(UIControlledApplication application)
    {
        try
        {
            application.CreateRibbonTab(RevitIds.RibbonTabName);
        }
        catch
        {
            // Tab may already exist.
        }

        var panel = application.GetRibbonPanels(RevitIds.RibbonTabName)
            .FirstOrDefault(x => string.Equals(x.Name, RevitIds.RibbonPanelName, StringComparison.Ordinal))
            ?? application.CreateRibbonPanel(RevitIds.RibbonTabName, RevitIds.RibbonPanelName);

        var assemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;

        var runButton = new PushButtonData(
            "ClashDetectorRun",
            "Run Clash",
            assemblyPath,
            typeof(RunClashDetectionCommand).FullName);
        runButton.ToolTip = "Run clash detection for active configs.";
        panel.AddItem(runButton);

        var paneButton = new PushButtonData(
            "ClashDetectorPane",
            "Open Pane",
            assemblyPath,
            typeof(ShowPaneCommand).FullName);
        paneButton.ToolTip = "Open the Clash Detector pane.";
        panel.AddItem(paneButton);
    }

    private static void OnDocumentChanged(object? sender, DocumentChangedEventArgs args)
    {
        var context = ClashAddinContext.Instance;
        if (!context.AutoDetectionCoordinator.IsAutoModeEnabled)
        {
            return;
        }

        var changedIds = args.GetAddedElementIds()
            .Concat(args.GetModifiedElementIds())
            .Select(x => x.IntegerValue)
            .Distinct()
            .ToArray();

        context.AutoDetectionCoordinator.RegisterChange("host", changedIds);
    }

    private static void OnIdling(object? sender, IdlingEventArgs args)
    {
        var context = ClashAddinContext.Instance;
        if (!context.AutoDetectionCoordinator.TryDequeueTrigger(out var trigger))
        {
            return;
        }

        context.QueueRequest(new RevitRequest
        {
            Type = RevitRequestType.RunAuto,
            ChangedElementIdsByModel = trigger.ChangedElementIdsByModel,
        });
    }
}
