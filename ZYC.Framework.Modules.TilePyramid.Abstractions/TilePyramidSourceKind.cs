namespace ZYC.Framework.Modules.TilePyramid.Abstractions;

/// <summary>Identifies the reader or generator used to open a pyramid.</summary>
public enum TilePyramidSourceKind
{
    /// <summary>A local or HTTP TIFF pyramid.</summary>
    [TilePyramidRoute("tiff")] [TilePyramidSourceIcon(TilePyramidSourceIcon.Tiff)]
    Tiff,

    /// <summary>A local or HTTP Aperio SVS slide.</summary>
    [TilePyramidRoute("svs")] [TilePyramidSourceIcon(TilePyramidSourceIcon.Svs)]
    Svs,

    /// <summary>A local Hamamatsu NDPI, VMS or VMU slide.</summary>
    [TilePyramidRoute("hamamatsu")] [TilePyramidSourceIcon(TilePyramidSourceIcon.Hamamatsu)]
    Hamamatsu,

    /// <summary>A local MIRAX slide with its companion data directory.</summary>
    [TilePyramidRoute("mirax")] [TilePyramidSourceIcon(TilePyramidSourceIcon.Mirax)]
    Mirax,

    /// <summary>A local or HTTP Deep Zoom descriptor.</summary>
    [TilePyramidRoute("deepzoom")] [TilePyramidSourceIcon(TilePyramidSourceIcon.DeepZoom)]
    DeepZoom,

    /// <summary>The built-in Mandelbrot generator.</summary>
    [TilePyramidRoute("mandelbrot")] [TilePyramidSourceIcon(TilePyramidSourceIcon.Mandelbrot)]
    Mandelbrot,

    /// <summary>An HTTP server providing Mandelbrot tiles.</summary>
    [TilePyramidRoute("http-mandelbrot")] [TilePyramidSourceIcon(TilePyramidSourceIcon.Mandelbrot)]
    HttpMandelbrot,

    /// <summary>The built-in sine and cosine plot.</summary>
    [TilePyramidRoute("function")] [TilePyramidSourceIcon(TilePyramidSourceIcon.Function)]
    Function
}
