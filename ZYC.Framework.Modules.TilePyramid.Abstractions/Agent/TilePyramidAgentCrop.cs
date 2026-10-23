using ZYC.TilePyramid.Core.Viewports;

namespace ZYC.Framework.Modules.TilePyramid.Abstractions.Agent;

/// <summary>
///     Exact crop mapping. Pixel-edge coordinates map as imageX = Bounds.X + cropX * Bounds.Width / PixelWidth, and
///     likewise for Y. No viewport rotation is applied.
/// </summary>
public record TilePyramidAgentCrop(
    Guid CropId,
    Guid DocumentId,
    ImageRect Bounds,
    int PixelWidth,
    int PixelHeight,
    int FocalPlaneIndex);