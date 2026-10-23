using System.Net.Http;
using ZYC.Framework.Abstractions.Overlay;
using ZYC.TilePyramid.Annotations.Wpf;

namespace ZYC.Framework.Modules.TilePyramid.UI.Annotations;

internal class AnnotationThumbnailView : AnnotationThumbnail
{
    private static readonly IOverlayManager OverlayManager = new AnnotationThumbnailOverlayManager();
    private IOverlay? _overlay;

    public AnnotationThumbnailView()
    {
        HttpClient = ThumbnailHttpClient;
        Loaded += (_, _) => _overlay ??= OverlayManager.Show(this);
        Unloaded += (_, _) =>
        {
            _overlay?.Dispose();
            _overlay = null;
        };
    }

    private static HttpClient ThumbnailHttpClient => TilePyramidDocumentTools.SourceHttpClient;
}