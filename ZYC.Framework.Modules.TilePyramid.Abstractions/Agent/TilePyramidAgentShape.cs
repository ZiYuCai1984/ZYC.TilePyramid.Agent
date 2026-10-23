using ZYC.TilePyramid.Core.Viewports;

namespace ZYC.Framework.Modules.TilePyramid.Abstractions.Agent;

/// <summary>Explicit geometry DTO: rectangles and ellipses use Region only; polygons use Vertices only.</summary>
public record TilePyramidAgentShape
{
    /// <summary>The geometry kind.</summary>
    public TilePyramidAgentShapeKind Kind { get; init; }
    /// <summary>Bounding rectangle for a rectangle or ellipse; null for polygons.</summary>
    public ImageRect? Region { get; init; }
    /// <summary>Polygon vertices; empty for rectangles and ellipses.</summary>
    public ImagePoint[] Vertices { get; init; } = [];
}