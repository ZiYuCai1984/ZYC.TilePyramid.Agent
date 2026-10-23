namespace ZYC.Framework.Modules.TilePyramid;

internal class TilePyramidRouteParameters
{
    public TilePyramidRouteParameters(
        Uri? source = null,
        double? x = null,
        double? y = null,
        double? scale = null,
        double? rotation = null,
        string? format = null,
        int? plane = null)
    {
        Source = source;
        X = x;
        Y = y;
        Scale = scale;
        Rotation = rotation;
        Format = format;
        Plane = plane;
    }

    public Uri? Source { get; }
    public double? X { get; }
    public double? Y { get; }
    public double? Scale { get; }
    public double? Rotation { get; }
    public string? Format { get; }
    public int? Plane { get; }
}
