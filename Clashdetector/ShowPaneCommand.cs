using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Clashdetector.Revit;

namespace Clashdetector;

[Transaction(TransactionMode.Manual)]
public sealed class ShowPaneCommand : IExternalCommand
{
    public Result Execute(
        ExternalCommandData commandData,
        ref string message,
        Autodesk.Revit.DB.ElementSet elements)
    {
        var context = ClashAddinContext.Instance;
        context.ShowPane(commandData.Application);
        context.QueueRequest(new RevitRequest
        {
            Type = RevitRequestType.RefreshCatalogs,
        });
        return Result.Succeeded;
    }
}
