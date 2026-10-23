using ZYC.TilePyramid.Core.Viewports;

namespace ZYC.Framework.Modules.TilePyramid.Abstractions.Agent;

/// <summary>
///     Actual view geometry in full-resolution image pixels. View dimensions are WPF view units. The visible quad may
///     extend outside the image.
/// </summary>
public record TilePyramidAgentViewport(
    Guid DocumentId,
    TilePyramidNavigationState? Navigation,
    int FocalPlaneIndex,
    bool IsInitialized,
    bool IsVisible,
    double ViewWidth,
    double ViewHeight,
    ImageQuad? VisibleQuad,
    double? MinimumScale,
    double? MaximumScale);