using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using MahApps.Metro.IconPacks;
using ZYC.TilePyramid.Annotations;

namespace ZYC.Framework.Modules.TilePyramid.UI.Annotations;

internal partial class AnnotationPropertiesDialog
{
    private readonly CompositeDisposable _bindings = new();
    private string _colorText = "";
    private uint? _parsedColor;
    private Brush _previewBrush = Brushes.Black;
    private string? _selectedPredefinedColor;
    private string? _selectedPredefinedIcon;

    public string ColorText
    {
        get => _colorText;
        set => SetProperty(ref _colorText, value);
    }

    public string? SelectedPredefinedColor
    {
        get => _selectedPredefinedColor;
        set
        {
            if (SetProperty(ref _selectedPredefinedColor, value) && value is not null)
            {
                ColorText = value;
            }
        }
    }

    public string? SelectedPredefinedIcon
    {
        get => _selectedPredefinedIcon;
        set
        {
            if (SetProperty(ref _selectedPredefinedIcon, value) && value is not null)
            {
                Draft.Icon = value;
            }
        }
    }

    public Brush PreviewBrush => _previewBrush;
    public bool CanConfirm => _parsedColor.HasValue && SelectedLayer?.IsSelectable == true;

    public string ValidationMessage => !_parsedColor.HasValue
        ? "Enter a valid hexadecimal color, such as #38BDF8 or #FF38BDF8."
        : SelectedLayer?.IsSelectable != true
            ? "Select an available layer; locked layers cannot be modified, and hidden layers cannot contain new annotations."
            : "";

    public IReadOnlyList<LayerChoice> LayerOptions { get; private set; } = [];

    public LayerChoice? SelectedLayer
    {
        get => LayerOptions.FirstOrDefault(layer => layer.Id == Draft.LayerId);
        set
        {
            if (value is not { IsSelectable: true }) return;
            Draft.LayerId = value.Id;
        }
    }

    public record LayerChoice(string Id, string DisplayName, bool IsSelectable);

    private void InitializeBindings()
    {
        ObservePropertyChanges(_request.Layers).StartWith("")
            .Subscribe(_ => UpdateLayerOptions()).DisposeWith(_bindings);
        ObservePropertyChanges(Draft)
            .Where(name => name is null or "" or nameof(Annotation.LayerId))
            .Subscribe(_ => NotifyLayerSelection()).DisposeWith(_bindings);

        ObservePropertyChanges(this)
            .Where(name => name is null or "" or nameof(ColorText))
            .Select(_ => ColorText)
            .StartWith(ColorText)
            .DistinctUntilChanged()
            .Subscribe(UpdateColor)
            .DisposeWith(_bindings);

        ObservePropertyChanges(Draft)
            .Where(name => name is null or "" or nameof(Annotation.Icon))
            .Select(_ => Draft.Icon)
            .StartWith(Draft.Icon)
            .DistinctUntilChanged()
            .Subscribe(_ => UpdateSelectedPredefinedIcon())
            .DisposeWith(_bindings);

        // ReSharper disable once SuspiciousTypeConversion.Global
        ObservePropertyChanges((INotifyPropertyChanged)TilePyramidConfig)
            .Subscribe(name =>
            {
                if (name is null or "" or nameof(TilePyramidConfig.PredefinedColors))
                {
                    UpdateSelectedPredefinedColor();
                }

                if (name is null or "" or nameof(TilePyramidConfig.PredefinedIcons))
                {
                    UpdateSelectedPredefinedIcon();
                }
            })
            .DisposeWith(_bindings);
    }

    private void UpdateColor(string text)
    {
        _parsedColor = AnnotationColorTools.TryParse(text, out var argb) ? argb : null;
        if (_parsedColor.HasValue)
        {
            Draft.StrokeArgb = argb;
            SetProperty(ref _previewBrush, AnnotationColorTools.CreateBrush(argb), nameof(PreviewBrush));
        }

        // Incomplete input keeps the last valid preview, but cannot be confirmed.
        UpdateSelectedPredefinedColor();
        OnPropertyChanged(nameof(CanConfirm));
        OnPropertyChanged(nameof(ValidationMessage));
    }

    private void UpdateLayerOptions()
    {
        var layers = _request.Layers;
        LayerOptions = layers.Definitions.OrderByDescending(layer => layer.Order)
            .ThenByDescending(layer => layer.Id, StringComparer.Ordinal)
            .Select(layer => new LayerChoice(layer.Id, AnnotationLayerItem.GetDisplayName(layers, layer.Id),
                !layers.IsLocked(layer.Id) && (!_request.IsCreating || layers.IsVisible(layer.Id))))
            .ToArray();
        OnPropertyChanged(nameof(LayerOptions));
        NotifyLayerSelection();
    }

    private void NotifyLayerSelection()
    {
        OnPropertyChanged(nameof(SelectedLayer));
        OnPropertyChanged(nameof(CanConfirm));
        OnPropertyChanged(nameof(ValidationMessage));
    }

    private void UpdateSelectedPredefinedColor()
    {
        var selected = _parsedColor is { } argb
            ? TilePyramidConfig.PredefinedColors.FirstOrDefault(value =>
                AnnotationColorTools.TryParse(value, out var presetArgb) && presetArgb == argb)
            : null;

        // Matching a preset only updates the selection; it must not rewrite the user's text.
        SetProperty(ref _selectedPredefinedColor, selected, nameof(SelectedPredefinedColor));
    }

    private void UpdateSelectedPredefinedIcon()
    {
        var icon = Draft.Icon.Trim();
        // HybridIcon accepts Material names without regard to case; image data and URIs retain their case.
        var comparison = Enum.TryParse<PackIconMaterialKind>(icon, true, out _)
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        var selected = TilePyramidConfig.PredefinedIcons.FirstOrDefault(value =>
            string.Equals(value.Trim(), icon, comparison));
        SetProperty(ref _selectedPredefinedIcon, selected, nameof(SelectedPredefinedIcon));
    }

    private static IObservable<string?> ObservePropertyChanges(INotifyPropertyChanged source)
    {
        return Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                handler => source.PropertyChanged += handler,
                handler => source.PropertyChanged -= handler)
            .Select(change => change.EventArgs.PropertyName);
    }

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
