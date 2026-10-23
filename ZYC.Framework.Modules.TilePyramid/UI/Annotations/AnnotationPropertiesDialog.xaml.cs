using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using ZYC.CoreToolkit.Extensions.Autofac.Attributes;
using ZYC.Framework.Core;
using ZYC.Framework.Modules.TilePyramid.Abstractions;
using ZYC.TilePyramid.Annotations;
using ZYC.TilePyramid.Core.Sources;

namespace ZYC.Framework.Modules.TilePyramid.UI.Annotations;

[Register]
internal partial class AnnotationPropertiesDialog : INotifyPropertyChanged
{
    private readonly AnnotationPropertiesRequest _request;

    private Annotation? _confirmedAnnotation;

    public AnnotationPropertiesDialog(
        AnnotationPropertiesRequest request,
        TilePyramidConfig tilePyramidConfig)
    {
        TilePyramidConfig = tilePyramidConfig;
        _request = request;
        _colorText = $"#{Draft.StrokeArgb:X8}";
        InitializeComponent();
        InitializeBindings();
        request.CloseRequested += OnCloseRequested;
    }

    public TilePyramidConfig TilePyramidConfig { get; }


    public Annotation Draft => _request.Draft;
    public string AnnotationTitle => _request.Title;
    public string ConfirmText => _request.ConfirmText;
    public string CancelText => _request.CancelText;
    public ITilePyramidSource PreviewSource => _request.PreviewSource;
    public int PreviewFocalPlaneIndex => _request.PreviewFocalPlaneIndex;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);

        LabelInput.FocusAndSelectAll();
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            Close();
            return;
        }

        if (e.Key == Key.Enter && e.IsRepeat)
        {
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter
            && Keyboard.FocusedElement is Emoji.Wpf.RichTextBox { AcceptsReturn: false } textBox)
        {
            e.Handled = true;
            //!WARNING Emoji.Wpf.TextBox forwards its internal RichTextBox.Text only on LostFocus.
            // Enter does not move focus; flush the internal binding before reading Draft.Label/Icon.
            // AcceptsReturn excludes the multiline notes input so Enter can still insert a newline.
            textBox.GetBindingExpression(Emoji.Wpf.RichTextBox.TextProperty)?.UpdateSource();
            Confirm_OnClick(this, e);
            return;
        }

        base.OnPreviewKeyDown(e);
    }

    private void Confirm_OnClick(object sender, RoutedEventArgs e)
    {
        if (!CanConfirm)
        {
            if (!_parsedColor.HasValue)
            {
                ColorInput.Focus();
            }
            else
            {
                LayerInput.Focus();
            }

            return;
        }

        Draft.Label = Draft.Label.Trim();
        Draft.Icon = Draft.Icon.Trim();
        _confirmedAnnotation = Draft;
        Close();
    }

    private void Cancel_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnCloseRequested(object? sender, EventArgs e)
    {
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        _request.CloseRequested -= OnCloseRequested;
        _bindings.Dispose();
        base.OnClosed(e);
        _request.Complete(_confirmedAnnotation);
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
