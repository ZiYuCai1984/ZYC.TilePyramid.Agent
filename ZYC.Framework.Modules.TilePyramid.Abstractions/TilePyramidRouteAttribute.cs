namespace ZYC.Framework.Modules.TilePyramid.Abstractions;

/// <summary>Specifies the first URI path segment identifying a pyramid source.</summary>
[AttributeUsage(AttributeTargets.Field)]
public class TilePyramidRouteAttribute : Attribute
{
    public TilePyramidRouteAttribute(string segment)
    {
        Segment = segment;
    }

    /// <summary>Gets the route segment without leading or trailing slashes.</summary>
    public string Segment { get; }
}
