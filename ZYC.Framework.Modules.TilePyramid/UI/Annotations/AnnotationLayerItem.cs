using ZYC.Framework.Core.Localizations;
using ZYC.TilePyramid.Annotations;

namespace ZYC.Framework.Modules.TilePyramid.UI.Annotations;

/// <summary>A sidebar row over the document's shared layer settings.</summary>
internal class AnnotationLayerItem : ObservableObject
{
    private readonly AnnotationLayerSettings _settings;
    private string _displayName;
    private bool _isLocked;
    private bool _isVisible;

    public AnnotationLayerItem(AnnotationLayerDefinition definition, AnnotationLayerSettings settings)
    {
        _settings = settings;
        Definition = definition;
        _displayName = DisplayName;
        _isVisible = IsVisible;
        _isLocked = IsLocked;
    }

    public AnnotationLayerDefinition Definition { get; private set; }
    public string Id => Definition.Id;
    public string DisplayName => GetDisplayName(_settings, Id);

    public string Name
    {
        get => DisplayName;
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                _settings.Rename(Id, value);
            }

            OnPropertyChanged();
        }
    }

    public bool IsVisible
    {
        get => _settings.IsVisible(Id);
        set => _settings.SetVisible(Id, value);
    }

    public bool IsLocked
    {
        get => _settings.IsLocked(Id);
        set => _settings.SetLocked(Id, value);
    }

    public bool IsDefault => string.IsNullOrEmpty(Id);

    public void Refresh(AnnotationLayerDefinition definition)
    {
        if (!ReferenceEquals(Definition, definition))
        {
            Definition = definition;
            OnPropertyChanged(nameof(Definition));
        }

        if (Set(ref _displayName, DisplayName, nameof(DisplayName)))
        {
            OnPropertyChanged(nameof(Name));
        }

        Set(ref _isVisible, IsVisible, nameof(IsVisible));
        Set(ref _isLocked, IsLocked, nameof(IsLocked));
    }

    public static string GetDisplayName(AnnotationLayerSettings settings, string id)
    {
        var name = settings.GetName(id);
        return string.IsNullOrEmpty(name) ? L.T("Default") : name;
    }
}