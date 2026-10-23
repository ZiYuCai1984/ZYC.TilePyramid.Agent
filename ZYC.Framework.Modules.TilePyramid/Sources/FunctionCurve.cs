using ZYC.TilePyramid.Core.Imaging;

namespace ZYC.Framework.Modules.TilePyramid.Sources;

/// <summary>One curve and its appearance within a shared coordinate system.</summary>
/// <param name="Function">
///     Evaluates y = f(x). Must be deterministic and safe for concurrent calls across tiles.
///     Return NaN or infinity for undefined points; exceptions fail the tile request.
/// </param>
internal record FunctionCurve(Func<double, double> Function)
{
    public string Name { get; init; } = "Function";

    public Rgba32 Color { get; init; } = new(56, 189, 248);

    /// <summary>Stroke width in pyramid-level pixels, greater than zero and at most 256.</summary>
    public double Thickness { get; init; } = 2.5;
}
