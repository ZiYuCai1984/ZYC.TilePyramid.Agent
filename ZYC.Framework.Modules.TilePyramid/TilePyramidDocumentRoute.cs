using System.IO;
using System.Web;
using ZYC.Framework.Abstractions;
using ZYC.Framework.Core;
using ZYC.Framework.Modules.TilePyramid.Abstractions;
using ZYC.TilePyramid.Core.Viewports;

namespace ZYC.Framework.Modules.TilePyramid;

internal class TilePyramidDocumentRoute
{
    private TilePyramidDocumentRoute(TilePyramidSourceKind kind, TilePyramidRouteParameters parameters)
    {
        Kind = kind;
        Format = parameters.Format;
        if (parameters.Plane is < 0)
        {
            throw new FormatException("The focal-plane index must be non-negative.");
        }

        InitialFocalPlaneIndex = parameters.Plane;
        var viewportValues = new[] { parameters.X, parameters.Y, parameters.Scale, parameters.Rotation };
        if (viewportValues.Any(v => v.HasValue))
        {
            if (viewportValues.Any(v => !v.HasValue))
            {
                throw new FormatException("A viewport requires x, y, scale and rotation together.");
            }

            InitialNavigation = new TilePyramidNavigationState(
                new ImagePoint(parameters.X!.Value, parameters.Y!.Value),
                parameters.Scale!.Value,
                parameters.Rotation!.Value);
        }

        DocumentUri = TilePyramidModuleConstants.CreateDocumentUri(kind, parameters.Source, format: Format);
        // Read back the normalized source address, including HTTP Mandelbrot's trailing slash.
        Source = UriBinder.Bind<TilePyramidRouteParameters>(DocumentUri).Source;
    }

    public TilePyramidSourceKind Kind { get; }
    public Uri? Source { get; }
    public string? Format { get; }
    public Uri DocumentUri { get; }
    public TilePyramidNavigationState? InitialNavigation { get; }
    public int? InitialFocalPlaneIndex { get; }
    public string StateKey => DocumentUri.AbsoluteUri;

    public string? LocalStatePath => Source is { IsFile: true }
        ? Path.Combine(Path.GetDirectoryName(Source.LocalPath)!, $".{ProductInfo.Scheme}",
            $"{Path.GetFileName(Source.LocalPath)}.tilepyramid.json")
        : null;

    public Uri CreateViewUri(TilePyramidNavigationState? navigation, int? focalPlaneIndex = null)
    {
        return TilePyramidModuleConstants.CreateDocumentUri(Kind, Source, navigation, Format, focalPlaneIndex);
    }

    public static TilePyramidDocumentRoute Parse(Uri uri)
    {
        if (!uri.IsAbsoluteUri || uri.Scheme != ProductInfo.Scheme
                               || uri.Host != TilePyramidModuleConstants.Host || uri.Fragment.Length != 0
                               || uri.UserInfo.Length != 0 || uri.Port != -1)
        {
            throw new FormatException($"Invalid TilePyramid URI: '{uri}'.");
        }

        var kind = TilePyramidSourceKindTools.FromRoutePath(uri.AbsolutePath);

        var query = HttpUtility.ParseQueryString(uri.Query);
        foreach (var key in query.AllKeys)
        {
            var values = key is null ? null : query.GetValues(key);
            if (key?.ToLowerInvariant() is not ("source" or "x" or "y" or "scale" or "rotation" or "format" or "plane")
                || values is not { Length: 1 } || string.IsNullOrWhiteSpace(values[0]))
            {
                throw new FormatException($"Invalid TilePyramid query parameter: '{key}'.");
            }
        }

        return new TilePyramidDocumentRoute(kind, UriBinder.Bind<TilePyramidRouteParameters>(uri));
    }

    public static bool TryParse(Uri uri, out TilePyramidDocumentRoute? route)
    {
        try
        {
            route = Parse(uri);
            return true;
        }
        catch (Exception e) when (e is FormatException or ArgumentException)
        {
            route = null;
            return false;
        }
    }
}
