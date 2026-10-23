using System.Reflection;
using ZYC.Framework.Abstractions;

namespace ZYC.Framework.Modules.TilePyramid.Abstractions;

/// <summary>Resolves the URI routes and icons declared on pyramid source kinds.</summary>
public static class TilePyramidSourceKindTools
{
    private static readonly Dictionary<TilePyramidSourceKind, string> RoutePaths =
        Enum.GetValues<TilePyramidSourceKind>().ToDictionary(kind => kind, kind =>
            typeof(TilePyramidSourceKind).GetField(kind.ToString())!
                .GetCustomAttribute<TilePyramidRouteAttribute>()?.Segment
            ?? throw new InvalidOperationException($"Missing route attribute for TilePyramid source kind '{kind}'."));

    private static readonly Dictionary<string, TilePyramidSourceKind> SourceKinds =
        RoutePaths.ToDictionary(pair => pair.Value, pair => pair.Key, StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<TilePyramidSourceKind, string> Icons =
        Enum.GetValues<TilePyramidSourceKind>().ToDictionary(kind => kind, kind =>
            typeof(TilePyramidSourceKind).GetField(kind.ToString())!
                .GetCustomAttribute<TilePyramidSourceIcon>()?.Icon
            ?? throw new InvalidOperationException($"Missing icon attribute for TilePyramid source kind '{kind}'."));

    /// <summary>Gets the icon declared on a source kind.</summary>
    public static string GetIcon(TilePyramidSourceKind kind)
    {
        return Icons.TryGetValue(kind, out var icon)
            ? icon
            : throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
    }

    /// <summary>Gets the URI route segment declared on a source kind.</summary>
    public static string GetRoutePath(TilePyramidSourceKind kind)
    {
        return RoutePaths.TryGetValue(kind, out var path)
            ? path
            : throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
    }

    /// <summary>Resolves the first URI path segment to a source kind, ignoring case.</summary>
    public static TilePyramidSourceKind FromRoutePath(string routePath)
    {
        var path = UriTools.NormalizeUriPath(routePath).TrimStart('/');
        var separatorIndex = path.IndexOf('/');
        var segment = separatorIndex < 0 ? path : path[..separatorIndex];

        return SourceKinds.TryGetValue(segment, out var kind)
            ? kind
            : throw new FormatException($"Unknown TilePyramid route: '{routePath}'.");
    }
}