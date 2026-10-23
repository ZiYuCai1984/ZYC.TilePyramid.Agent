using ZYC.TilePyramid.Core.Imaging;
using ZYC.TilePyramid.Core.Sources;

namespace ZYC.Framework.Modules.TilePyramid.Sources;

/// <summary>
///     Renders multiple functions in a shared coordinate system at each pyramid level.
/// </summary>
internal class FunctionTilePyramidSource : TilePyramidSource
{
    private const long ImageWidth = 1L << 30;
    private const long ImageHeight = ImageWidth / 2;
    private const int TileSize = 256;
    private const int TileOverlap = 1;
    private const double SampleStep = 0.5;
    private const double CurveTolerance = 0.25;
    private const int MaximumSubdivisionDepth = 8;

    public FunctionTilePyramidSource(FunctionTilePyramidOptions options)
        : base(CreateInfo(options), CreateMetadata(options))
    {
        Options = options;
    }

    public FunctionTilePyramidOptions Options { get; }

    protected override ValueTask<TileResource> ResolveTileCoreAsync(
        TileKey key,
        CancellationToken cancellationToken)
    {
        return new ValueTask<TileResource>(Task.Run(
            () => RenderTile(key, cancellationToken), cancellationToken));
    }

    private TileResource RenderTile(TileKey key, CancellationToken cancellationToken)
    {
        var level = Info.GetLevel(key.Level);
        var left = checked(key.Column * level.TileWidth) - (key.Column == 0 ? 0 : TileOverlap);
        var top = checked(key.Row * level.TileHeight) - (key.Row == 0 ? 0 : TileOverlap);
        var width = (int)Math.Min(
            level.TileWidth + (key.Column == 0 ? 1 : 2) * TileOverlap, level.Width - left);
        var height = (int)Math.Min(
            level.TileHeight + (key.Row == 0 ? 1 : 2) * TileOverlap, level.Height - top);
        var stride = checked(width * 4);
        var pixels = new byte[checked(stride * height)];
        var unitsPerPixelX = (Options.MaximumX - Options.MinimumX) / ImageWidth * level.DownsampleX;
        var unitsPerPixelY = (Options.MaximumY - Options.MinimumY) / ImageHeight * level.DownsampleY;
        var gridStepX = GetGridStep(unitsPerPixelX);
        var gridStepY = GetGridStep(unitsPerPixelY);

        for (var pixelY = 0; pixelY < height; pixelY++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var y = Options.MaximumY - (top + pixelY + 0.5) * unitsPerPixelY;
            var minorY = GridCoverage(y, gridStepY / 5, unitsPerPixelY);
            var majorY = GridCoverage(y, gridStepY, unitsPerPixelY);
            var axisY = Coverage(Math.Abs(y) / unitsPerPixelY, 0.75);

            for (var pixelX = 0; pixelX < width; pixelX++)
            {
                var x = Options.MinimumX + (left + pixelX + 0.5) * unitsPerPixelX;
                var color = Options.BackgroundColor;
                if (Options.ShowGrid)
                {
                    color = Blend(color, Options.MinorGridColor,
                        Math.Max(minorY, GridCoverage(x, gridStepX / 5, unitsPerPixelX)));
                    color = Blend(color, Options.MajorGridColor,
                        Math.Max(majorY, GridCoverage(x, gridStepX, unitsPerPixelX)));
                }

                if (Options.ShowAxes)
                {
                    color = Blend(color, Options.AxisColor,
                        Math.Max(axisY, Coverage(Math.Abs(x) / unitsPerPixelX, 0.75)));
                }

                var offset = pixelY * stride + pixelX * 4;
                pixels[offset] = color.Blue;
                pixels[offset + 1] = color.Green;
                pixels[offset + 2] = color.Red;
                pixels[offset + 3] = color.Alpha;
            }
        }

        var coverage = new double[width * height];
        foreach (var curve in Options.Curves)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RenderCurve(curve);
        }

        cancellationToken.ThrowIfCancellationRequested();
        return new TileResource.PixelBuffer(pixels, width, height, stride, TilePixelFormat.Bgra32);

        void RenderCurve(FunctionCurve curve)
        {
            // Union segments per curve, then composite each curve in collection order.
            Array.Clear(coverage);
            var curveRadius = curve.Thickness / 2;

            // A global half-pixel lattice and a stroke halo keep adjacent tiles consistent.
            var padding = (int)Math.Ceiling(curveRadius + 0.5);
            var previousX = (double)-padding;
            var previousY = EvaluateY(left + previousX);
            for (var pixelX = previousX + SampleStep; pixelX <= width + padding; pixelX += SampleStep)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var pixelY = EvaluateY(left + pixelX);
                DrawCurve(previousX, previousY, pixelX, pixelY, 0);
                previousX = pixelX;
                previousY = pixelY;
            }

            for (var index = 0; index < coverage.Length; index++)
            {
                if (coverage[index] == 0)
                {
                    continue;
                }

                var offset = index * 4;
                var color = Blend(new Rgba32(
                        pixels[offset + 2], pixels[offset + 1], pixels[offset], pixels[offset + 3]),
                    curve.Color, coverage[index]);
                pixels[offset] = color.Blue;
                pixels[offset + 1] = color.Green;
                pixels[offset + 2] = color.Red;
                pixels[offset + 3] = color.Alpha;
            }

            double EvaluateY(double levelX)
            {
                // The stroke halo must not evaluate the delegate outside its declared domain.
                if (levelX < 0 || levelX > ImageWidth / level.DownsampleX)
                {
                    return double.NaN;
                }

                var x = Math.Clamp(Options.MinimumX + levelX * unitsPerPixelX,
                    Options.MinimumX, Options.MaximumX);
                var value = curve.Function(x);
                return double.IsFinite(value)
                    ? (Options.MaximumY - value) / unitsPerPixelY - top
                    : double.NaN;
            }

            void DrawCurve(double x1, double y1, double x2, double y2, int depth)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!double.IsFinite(y1) || !double.IsFinite(y2))
                {
                    return;
                }

                var middleX = (x1 + x2) / 2;
                var middleY = EvaluateY(left + middleX);
                if (!double.IsFinite(middleY))
                {
                    return;
                }

                // Refine curved intervals; leave unresolved jumps/poles disconnected.
                if (Math.Abs(middleY - (y1 / 2 + y2 / 2)) > CurveTolerance)
                {
                    if (depth < MaximumSubdivisionDepth)
                    {
                        DrawCurve(x1, y1, middleX, middleY, depth + 1);
                        DrawCurve(middleX, middleY, x2, y2, depth + 1);
                    }

                    return;
                }

                DrawSegment(coverage, width, height, x1, y1, x2, y2, curveRadius);
            }
        }
    }

    private static void DrawSegment(
        double[] coverage, int width, int height,
        double x1, double y1, double x2, double y2, double curveRadius)
    {
        var reach = curveRadius + 0.5;
        var left = (int)Math.Clamp(Math.Floor(Math.Min(x1, x2) - reach), 0, width);
        var right = (int)Math.Clamp(Math.Ceiling(Math.Max(x1, x2) + reach), 0, width);
        var top = (int)Math.Clamp(Math.Floor(Math.Min(y1, y2) - reach), 0, height);
        var bottom = (int)Math.Clamp(Math.Ceiling(Math.Max(y1, y2) + reach), 0, height);
        var dx = x2 - x1;
        var dy = y2 - y1;
        var lengthSquared = dx * dx + dy * dy;
        if (!double.IsFinite(lengthSquared))
        {
            return;
        }

        for (var y = top; y < bottom; y++)
        {
            for (var x = left; x < right; x++)
            {
                var px = x + 0.5 - x1;
                var py = y + 0.5 - y1;
                var position = Math.Clamp((px * dx + py * dy) / lengthSquared, 0, 1);
                var distanceX = px - position * dx;
                var distanceY = py - position * dy;
                var distance = Math.Sqrt(distanceX * distanceX + distanceY * distanceY);
                var index = y * width + x;
                // Union the segments before blending, so joins do not become darker or thicker.
                coverage[index] = Math.Max(coverage[index], Coverage(distance, curveRadius));
            }
        }
    }

    private static double GetGridStep(double unitsPerPixel)
    {
        var target = unitsPerPixel * 80;
        var magnitude = Math.Pow(10, Math.Floor(Math.Log10(target)));
        var normalized = target / magnitude;
        return (normalized <= 1 ? 1 : normalized <= 2 ? 2 : normalized <= 5 ? 5 : 10) * magnitude;
    }

    private static double GridCoverage(double value, double step, double unitsPerPixel)
    {
        var distance = Math.Abs(Math.IEEERemainder(value, step)) / unitsPerPixel;
        return Coverage(distance, 0.5);
    }

    private static double Coverage(double distance, double radius)
    {
        return Math.Clamp(radius + 0.5 - distance, 0, 1);
    }

    private static Rgba32 Blend(Rgba32 background, Rgba32 foreground, double coverage)
    {
        var foregroundAlpha = coverage * foreground.Alpha / 255;
        var backgroundAlpha = (1 - foregroundAlpha) * background.Alpha / 255;
        var alpha = foregroundAlpha + backgroundAlpha;
        if (alpha == 0)
        {
            return new Rgba32(0, 0, 0, 0);
        }

        return new Rgba32(
            (byte)Math.Round((background.Red * backgroundAlpha + foreground.Red * foregroundAlpha) / alpha),
            (byte)Math.Round((background.Green * backgroundAlpha + foreground.Green * foregroundAlpha) / alpha),
            (byte)Math.Round((background.Blue * backgroundAlpha + foreground.Blue * foregroundAlpha) / alpha),
            (byte)Math.Round(alpha * 255));
    }

    private static TilePyramidInfo CreateInfo(FunctionTilePyramidOptions options)
    {
        ValidateRange(options.MinimumX, options.MaximumX, ImageWidth, nameof(options.MinimumX));
        ValidateRange(options.MinimumY, options.MaximumY, ImageHeight, nameof(options.MinimumY));
        if (options.Curves.IsDefaultOrEmpty)
        {
            throw new ArgumentException("At least one function curve is required.", nameof(options.Curves));
        }

        foreach (var curve in options.Curves)
        {

            if (!double.IsFinite(curve.Thickness) || curve.Thickness <= 0 || curve.Thickness > TileSize)
            {
                throw new ArgumentOutOfRangeException(nameof(curve.Thickness),
                    "Curve thickness must be positive and no greater than one tile (256 level pixels).");
            }
        }

        var levels = new List<TilePyramidLevel>();
        var width = ImageWidth;
        var height = ImageHeight;
        var downsample = 1d;
        while (true)
        {
            levels.Add(new TilePyramidLevel(width, height, TileSize, TileSize,
                downsample, downsample, TileOverlap));
            if (width <= TileSize && height <= TileSize)
            {
                break;
            }

            width = (width + 1) / 2;
            height = (height + 1) / 2;
            downsample *= 2;
        }

        return new TilePyramidInfo(ImageWidth, ImageHeight, levels);
    }

    private static void ValidateRange(double minimum, double maximum, long pixels, string parameterName)
    {
        var span = maximum - minimum;
        if (!double.IsFinite(minimum) || !double.IsFinite(maximum)
                                      || !double.IsFinite(span) || span <= 0 || span / pixels == 0)
        {
            throw new ArgumentOutOfRangeException(parameterName,
                "The range must have finite increasing bounds and a representable positive pixel size.");
        }
    }

    private static TilePyramidMetadata CreateMetadata(FunctionTilePyramidOptions options)
    {
        var metadata = new TilePyramidMetadata
        {
            Name = options.Name,
            SourceFormat = "Procedural",
            Comments = FormattableString.Invariant(
                $"{options.Name}; {options.Curves.Length} curves; x in [{options.MinimumX:G}, {options.MaximumX:G}], y in [{options.MinimumY:G}, {options.MaximumY:G}]."),
            BackgroundColor = options.BackgroundColor
        };

        for (var index = 0; index < options.Curves.Length; index++)
        {
            metadata.AdditionalProperties.Add($"Curve {index + 1}", options.Curves[index].Name);
        }

        return metadata;
    }
}
