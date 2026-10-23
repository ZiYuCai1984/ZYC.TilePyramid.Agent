using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using ZYC.CoreToolkit.Extensions.Autofac.Attributes;
using ZYC.Framework.Abstractions;
using ZYC.Framework.Abstractions.Notification.Toast;

namespace ZYC.Framework.Modules.TilePyramid.UI.Annotations;

[Register]
internal partial class AnnotationPanelView
{
    public AnnotationPanelView(
        ILogger<AnnotationPanelView> logger,
        IToastManager toastManager)
    {
        Logger = logger;
        ToastManager = toastManager;

        InitializeComponent();
    }

    private ILogger<AnnotationPanelView> Logger { get; }

    private IToastManager ToastManager { get; }


    private void AnnotationItem_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        try
        {
            var element = (FrameworkElement)sender;

            var tilePyramidDocumentView = (TilePyramidDocumentView)element.Tag;
            var command = tilePyramidDocumentView.FocusAnnotationCommand;
            if (command.CanExecute(element.DataContext))
            {
                command.Execute(element.DataContext);
            }
        }
        catch (Exception exception)
        {
            Logger.Error(exception);
            ToastManager.PromptException(exception);
        }
    }
}