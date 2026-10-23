using System.Globalization;
using ZYC.Framework.Abstractions;
using ZYC.TilePyramid.Core.Viewports;

namespace ZYC.Framework.Modules.TilePyramid.Abstractions;

#pragma warning disable CS1591
public static class TilePyramidModuleConstants
{
    public const string Host = "tile";

    public const string Title = "TilePyramid";

    public const string Icon = "Pyramid";

    public const string FileDialogFilter =
        "Tile Pyramid Files (*.tif;*.tiff;*.svs;*.ndpi;*.vms;*.vmu;*.mrxs;*.dzi;*.xml)|*.tif;*.tiff;*.svs;*.ndpi;*.vms;*.vmu;*.mrxs;*.dzi;*.xml|"
        + "TIFF Files (*.tif;*.tiff)|*.tif;*.tiff|Aperio SVS Files (*.svs)|*.svs|"
        + "Hamamatsu Files (*.ndpi;*.vms;*.vmu)|*.ndpi;*.vms;*.vmu|"
        + "MIRAX Files (*.mrxs)|*.mrxs|"
        + "Deep Zoom Files (*.dzi;*.xml)|*.dzi;*.xml";


    public const string MCPToolName = "TilePyramid";


    public static string GetTabItemTitle(TilePyramidSourceKind kind, Uri? source)
    {
        if (source == null)
        {
            if (kind == TilePyramidSourceKind.Mandelbrot || kind == TilePyramidSourceKind.HttpMandelbrot)
            {
                return "Mandelbrot";
            }

            if (kind == TilePyramidSourceKind.Function)
            {
                return "sin(x) / cos(x)";
            }

            return Title;
        }

        if (source.IsFile)
        {
            return Path.GetFileName(source.LocalPath);
        }

        if (source.Scheme is "http" or "https")
        {
            var lastSegment = source.Segments.LastOrDefault(s => s != "/");
            if (!string.IsNullOrWhiteSpace(lastSegment))
            {
                return Uri.UnescapeDataString(lastSegment.TrimEnd('/'));
            }

            return source.Host;
        }

        return source.ToString();
    }


    public static Uri MandelbrotUri()
    {
        return CreateDocumentUri(TilePyramidSourceKind.Mandelbrot);
    }

    /// <summary>Creates a document link, optionally including an initial viewport and focal plane.</summary>
    /// <param name="kind">The source reader or generator.</param>
    /// <param name="source">The absolute source address; omitted for the built-in demos.</param>
    /// <param name="navigation">The initial viewport; omitted to restore the saved viewport.</param>
    /// <param name="format">The HTTP Mandelbrot tile extension; defaults to png.</param>
    /// <param name="focalPlaneIndex">The zero-based focal plane; omitted to restore the saved plane.</param>
    /// <returns>A canonical application URI.</returns>
    public static Uri CreateDocumentUri(
        TilePyramidSourceKind kind,
        Uri? source = null,
        TilePyramidNavigationState? navigation = null,
        string? format = null,
        int? focalPlaneIndex = null)
    {
        var path = TilePyramidSourceKindTools.GetRoutePath(kind);

        if (kind is TilePyramidSourceKind.Mandelbrot or TilePyramidSourceKind.Function)
        {
            if (source is not null)
            {
                throw new ArgumentException("The built-in demo has no source address.", nameof(source));
            }
        }
        else
        {
            if (source is not { IsAbsoluteUri: true }
                || !(source.IsFile || source.Scheme is "http" or "https")
                || source.Fragment.Length != 0
                || (source.IsFile && (source.Query.Length != 0 || source.AbsolutePath.EndsWith('/'))))
            {
                throw new ArgumentException("An absolute file or HTTP source address is required.", nameof(source));
            }

            if (kind == TilePyramidSourceKind.Hamamatsu && !source.IsFile)
            {
                throw new ArgumentException("Hamamatsu slides require a local file source.", nameof(source));
            }

            if (kind == TilePyramidSourceKind.Mirax && !source.IsFile)
            {
                throw new ArgumentException("MIRAX slides require a local file source.", nameof(source));
            }

            if (kind == TilePyramidSourceKind.HttpMandelbrot)
            {
                if (source.IsFile)
                {
                    throw new ArgumentException("HTTP Mandelbrot requires an HTTP source.", nameof(source));
                }

                var builder = new UriBuilder(source);
                if (!builder.Path.EndsWith('/'))
                {
                    builder.Path += '/';
                }

                source = builder.Uri;
            }
        }

        if (kind != TilePyramidSourceKind.HttpMandelbrot && format is not null)
        {
            throw new ArgumentException("Only HTTP Mandelbrot accepts a tile format.", nameof(format));
        }

        if (format is not null
            && (format.Length == 0 || format.Any(c => !char.IsAsciiLetterOrDigit(c))))
        {
            throw new ArgumentException("The tile format must be a file extension without a dot.", nameof(format));
        }

        if (focalPlaneIndex is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(focalPlaneIndex));
        }

        var query = new List<string>();
        if (source is not null)
        {
            query.Add($"source={Uri.EscapeDataString(source.AbsoluteUri)}");
        }

        if (format is not null and not "png")
        {
            query.Add($"format={Uri.EscapeDataString(format)}");
        }

        if (navigation is { } view)
        {
            // Also validate default(struct), which bypasses the navigation constructor.
            _ = new TilePyramidNavigationState(view.Center, view.Scale, view.RotationDegrees);
            query.Add($"x={FormatNumber(view.Center.X)}");
            query.Add($"y={FormatNumber(view.Center.Y)}");
            query.Add($"scale={FormatNumber(view.Scale)}");
            query.Add($"rotation={FormatNumber(view.RotationDegrees)}");
        }

        if (focalPlaneIndex is { } plane)
        {
            query.Add($"plane={plane.ToString(CultureInfo.InvariantCulture)}");
        }

        return UriTools.CreateAppUri(Host, path, string.Join('&', query));
    }

    private static string FormatNumber(double value)
    {
        return Uri.EscapeDataString(value.ToString("R", CultureInfo.InvariantCulture));
    }
}