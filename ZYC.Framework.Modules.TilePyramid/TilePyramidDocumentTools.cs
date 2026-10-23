using System.IO;
using System.Net.Http;
using ZYC.Framework.Core.Localizations;
using ZYC.Framework.Modules.TilePyramid.Abstractions;
using ZYC.Framework.Modules.TilePyramid.Sources;
using ZYC.TilePyramid.Core.Imaging;
using ZYC.TilePyramid.Core.Sources;

namespace ZYC.Framework.Modules.TilePyramid;

internal static class TilePyramidDocumentTools
{

    public static HttpClient SourceHttpClient { get; } = new();

    public static async Task<ITilePyramidSource> OpenSourceAsync(
        TilePyramidDocumentRoute route)
    {
        return route.Kind switch
        {
            TilePyramidSourceKind.Mandelbrot => new MandelbrotTilePyramidSource(),
            TilePyramidSourceKind.Function => new FunctionTilePyramidSource(new FunctionTilePyramidOptions(
                [
                    new FunctionCurve(Math.Sin) { Name = "y = sin(x)" },
                    new FunctionCurve(Math.Cos)
                    {
                        Name = "y = cos(x)",
                        Color = new Rgba32(251, 191, 36)
                    }
                ],
                -200 * Math.PI, 200 * Math.PI, -200, 200)
                {
                    Name = "sin(x) / cos(x)"
                }),
            TilePyramidSourceKind.Tiff => await TiffTilePyramidSource.OpenAsync(route.Source!, SourceHttpClient),
            TilePyramidSourceKind.Svs => await SvsTilePyramidSource.OpenAsync(route.Source!, SourceHttpClient),
            TilePyramidSourceKind.Hamamatsu => await HamamatsuTilePyramidSource.OpenAsync(route.Source!),
            TilePyramidSourceKind.Mirax => await MiraxTilePyramidSource.OpenAsync(route.Source!),
            TilePyramidSourceKind.DeepZoom => await DeepZoomTilePyramidSource.OpenAsync(route.Source!, SourceHttpClient),
            TilePyramidSourceKind.HttpMandelbrot => new HttpMandelbrotTilePyramidSource(
                route.Source!, route.Format ?? "png"),
            _ => throw new ArgumentOutOfRangeException(nameof(route))
        };
    }

    public static TilePyramidSourceKind GetTilePyramidSourceKindFromFilePath(string filePath)
    {
        return Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".tif" or ".tiff" => TilePyramidSourceKind.Tiff,
            ".svs" => TilePyramidSourceKind.Svs,
            ".ndpi" or ".vms" or ".vmu" => TilePyramidSourceKind.Hamamatsu,
            ".mrxs" => TilePyramidSourceKind.Mirax,
            ".dzi" or ".xml" => TilePyramidSourceKind.DeepZoom,
            _ => throw new NotSupportedException(
                L.T("Select a TIFF pyramid (.tif, .tiff), an Aperio slide (.svs), a Hamamatsu slide (.ndpi, .vms, .vmu), a MIRAX slide (.mrxs), or a Deep Zoom descriptor (.dzi, .xml)."))
        };
    }
}
