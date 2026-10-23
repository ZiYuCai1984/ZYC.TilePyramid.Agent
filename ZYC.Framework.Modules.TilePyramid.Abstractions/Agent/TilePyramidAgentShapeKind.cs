using System.Text.Json.Serialization;

namespace ZYC.Framework.Modules.TilePyramid.Abstractions.Agent;

/// <summary>Geometry supported by candidate submission.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<TilePyramidAgentShapeKind>))]
public enum TilePyramidAgentShapeKind
{
    /// <summary>An axis-aligned rectangle, specified by Region.</summary>
    Rectangle,
    /// <summary>An ellipse inscribed in Region.</summary>
    Ellipse,
    /// <summary>A closed polygon specified by at least three Vertices, without repeating the first point.</summary>
    Polygon
}