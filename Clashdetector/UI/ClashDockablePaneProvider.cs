using Autodesk.Revit.UI;

namespace Clashdetector.UI;

public sealed class ClashDockablePaneProvider : IDockablePaneProvider
{
    private readonly ClashPaneControl _control;

    public ClashDockablePaneProvider(ClashPaneControl control)
    {
        _control = control;
    }

    public void SetupDockablePane(DockablePaneProviderData data)
    {
        data.FrameworkElement = _control;
        data.InitialState = new DockablePaneState
        {
            DockPosition = DockPosition.Right,
            TabBehind = DockablePanes.BuiltInDockablePanes.ProjectBrowser,
        };
    }
}
