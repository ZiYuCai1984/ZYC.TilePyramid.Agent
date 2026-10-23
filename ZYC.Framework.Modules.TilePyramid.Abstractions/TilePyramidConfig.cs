using PropertyChanged;
using ZYC.CoreToolkit.Abstractions.Settings;
using ZYC.Framework.Abstractions.Config.Attributes;

namespace ZYC.Framework.Modules.TilePyramid.Abstractions;

#pragma warning disable CS1591


[AddINotifyPropertyChangedInterface]
public class TilePyramidConfig : IConfig
{
    //!WARNING Keep the first color manually aligned with AnnotationItem.DefaultStrokeArgb.
    public string[] PredefinedColors { get; set; } =
    [
        // Bright tones.
        "#FF38BDF8",
        "#FF2DD4BF",
        "#FFA3E635",
        "#FFFBBF24",
        "#FFFB923C",
        "#FFF87171",
        "#FFF472B6",
        "#FFA78BFA",
        // Deeper tones in the same hue order.
        "#FF0284C7",
        "#FF0D9488",
        "#FF65A30D",
        "#FFD97706",
        "#FFEA580C",
        "#FFDC2626",
        "#FFDB2777",
        "#FF7C3AED"
    ];

    public string[] PredefinedIcons { get; set; } =
    [
        "Rhombus",
        "Triangle",
        "Circle",
        "Star",
        "StarThreePoints",
        "StarFourPoints",
        "Hexagram",
        "Diamond",
        "Flag",
        "Information",
        "Alert",
        "MapMarker",
        "Target",
        "LightningBolt",
        "Heart",
        "Lightbulb"
    ];

    /// <summary>Gets or sets whether the active document's diagnostics are shown in the status bar.</summary>
    public bool ShowDiagnostics { get; set; } = true;

    /// <summary>
    ///     Gets or sets the default automatic acceptance policy captured when a recognition task starts.
    ///     Changing this value does not affect existing tasks or previously submitted results.
    /// </summary>
    public bool AutoAcceptRecognitionResults { get; set; }


    public bool DebugMode { get; set; }
}
