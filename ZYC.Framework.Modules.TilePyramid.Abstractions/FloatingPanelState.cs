using PropertyChanged;
using ZYC.TilePyramid.Core;

namespace ZYC.Framework.Modules.TilePyramid.Abstractions;

/// <summary>Stores a floating panel's placement and preferred outer size independently of the viewport.</summary>
[AddINotifyPropertyChangedInterface]
public class FloatingPanelState
{
    /// <summary>Gets or sets the panel's anchor on the editing surface.</summary>
    public FloatPosition Position { get; set; } = FloatPosition.TopLeft;

    /// <summary>Gets or sets the order within the anchor group, starting nearest the anchor.</summary>
    public int Order { get; set; }

    /// <summary>Gets or sets the user's visibility preference, excluding temporary editing suppression.</summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>Gets or sets the preferred outer width in DIP, or null to size to content.</summary>
    public double? Width { get; set; }

    /// <summary>Gets or sets the preferred outer height in DIP, or null to size to content.</summary>
    public double? Height { get; set; }

    /// <summary>Gets or sets whether the panel fills the available height instead of using its preferred height.</summary>
    public bool FillHeight { get; set; }

    /// <summary>Creates an independent copy for another tab or a persistence snapshot.</summary>
    public FloatingPanelState Clone()
    {
        return new FloatingPanelState
        {
            Position = Position, 
            Order = Order, 
            IsVisible = IsVisible, 
            Width = Width, 
            Height = Height,
            FillHeight = FillHeight
        };
    }
}