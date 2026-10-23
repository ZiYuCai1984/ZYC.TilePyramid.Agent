using System.Windows;
using ZYC.CoreToolkit.Extensions.Autofac.Attributes;

namespace ZYC.Framework.Modules.TilePyramid.UI.Debug;

[Register]
internal partial class DebugPanelView
{
    public DebugPanelView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is TilePyramidDocumentView document)
        {
            await document.AgentDebug.InitializeAsync();
        }
    }

    private async void OnAction(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string action, DataContext: AgentDebugSession session })
        {
            await session.ExecuteAsync(action);
        }
    }
}
