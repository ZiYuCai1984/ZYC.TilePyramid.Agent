using System.Collections.Immutable;
using ZYC.TilePyramid.Core.Imaging;

namespace ZYC.Framework.Modules.TilePyramid.Sources;

/// <summary>
///     Immutable construction parameters for one function plot. Create another source to change them.
/// </summary>
/// <param name="Curves">
///     Nonempty immutable collection of curves, drawn in order. Later curves appear on top.
/// </param>
/// <param name="MinimumX">Inclusive left bound of the function domain.</param>
/// <param name="MaximumX">Inclusive right bound of the function domain.</param>
/// <param name="MinimumY">Bottom of the visible mathematical range.</param>
/// <param name="MaximumY">Top of the visible mathematical range.</param>
internal record FunctionTilePyramidOptions(
    ImmutableArray<FunctionCurve> Curves,
    double MinimumX,
    double MaximumX,
    double MinimumY,
    double MaximumY)
{
    public string Name { get; init; } = "Function Plot";

    public Rgba32 BackgroundColor { get; init; } = new(15, 23, 42);
    public Rgba32 MinorGridColor { get; init; } = new(25, 36, 55);
    public Rgba32 MajorGridColor { get; init; } = new(42, 58, 79);
    public Rgba32 AxisColor { get; init; } = new(128, 148, 170);
    public bool ShowGrid { get; init; } = true;
    public bool ShowAxes { get; init; } = true;
}
