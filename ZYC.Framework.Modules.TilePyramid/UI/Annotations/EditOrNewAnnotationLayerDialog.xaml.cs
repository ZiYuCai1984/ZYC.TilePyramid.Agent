using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using ZYC.CoreToolkit.Extensions.Autofac.Attributes;
using ZYC.Framework.Abstractions;
using ZYC.Framework.Abstractions.Notification.Toast;
using ZYC.Framework.Core;
using ZYC.Framework.Core.Localizations;

namespace ZYC.Framework.Modules.TilePyramid.UI.Annotations;

[Register]
internal partial class EditOrNewAnnotationLayerDialog
{
    public EditOrNewAnnotationLayerDialog(
        EditOrNewAnnotationLayerRequest editOrNewAnnotationLayerRequest,
        TilePyramidDocumentView tilePyramidDocumentView,
        IToastManager toastManager,
        ILogger<EditOrNewAnnotationLayerDialog> logger)
    {
        TilePyramidDocumentView = tilePyramidDocumentView;
        ToastManager = toastManager;
        Logger = logger;

        EditOrNewAnnotationLayerRequest = editOrNewAnnotationLayerRequest;

        InitializeComponent();
        TilePyramidDocumentView.PropertyChanged += OnDocumentChanged;
        TilePyramidDocumentView.Unloaded += OnDocumentUnloaded;
    }


    public EditOrNewAnnotationLayerRequest EditOrNewAnnotationLayerRequest { get; }

    private TilePyramidDocumentView TilePyramidDocumentView { get; }

    private IToastManager ToastManager { get; }

    private ILogger<EditOrNewAnnotationLayerDialog> Logger { get; }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);

        LayerNameInput.FocusAndSelectAll();
    }


    private void Confirm_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(EditOrNewAnnotationLayerRequest.LayerName))
            {
                throw new ArgumentException(L.T("The layer name cannot be empty."));
            }

            if (EditOrNewAnnotationLayerRequest.IsEdit)
            {
                TilePyramidDocumentView.RenameAnnotationLayer(EditOrNewAnnotationLayerRequest.TargetLayer!,
                    EditOrNewAnnotationLayerRequest.LayerName);
            }
            else
            {
                TilePyramidDocumentView.AddAnnotationLayer(EditOrNewAnnotationLayerRequest.LayerName);
            }

            Close();
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            ToastManager.PromptException(ex);
            LayerNameInput.FocusZ();
        }
    }

    private void Cancel_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            Close();
            return;
        }

        if (e.Key == Key.Enter
            && Keyboard.FocusedElement is Emoji.Wpf.RichTextBox { AcceptsReturn: false } textBox)
        {
            e.Handled = true;
            if (!e.IsRepeat)
            {
                //!WARNING Emoji.Wpf.TextBox forwards its internal RichTextBox.Text only on LostFocus.
                // Enter does not move focus; flush the internal binding before validating LayerName.
                // Updating only LayerNameInput.Text's binding would still read the old text.
                textBox.GetBindingExpression(Emoji.Wpf.RichTextBox.TextProperty)?.UpdateSource();
                Confirm_OnClick(this, e);
            }

            return;
        }

        base.OnPreviewKeyDown(e);
    }

    private void OnDocumentChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!TilePyramidDocumentView.CanStartAnnotationEdit
            || (EditOrNewAnnotationLayerRequest.TargetLayer is { } layer
                && !TilePyramidDocumentView.CanModifyAnnotationLayer(layer)))
        {
            Close();
        }
    }

    private void OnDocumentUnloaded(object sender, RoutedEventArgs e)
    {
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        TilePyramidDocumentView.PropertyChanged -= OnDocumentChanged;
        TilePyramidDocumentView.Unloaded -= OnDocumentUnloaded;
        base.OnClosed(e);
    }
}
