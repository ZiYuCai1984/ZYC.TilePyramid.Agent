using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ZYC.Framework.Abstractions.Overlay;
using ZYC.TilePyramid.Annotations.Wpf;

namespace ZYC.Framework.Modules.TilePyramid.UI.Annotations;

/// <summary>A thumbnail-scoped overlay; Framework's window overlay would dim the entire application.</summary>
internal class AnnotationThumbnailOverlayManager : IOverlayManager
{
    public IOverlay Show(object target, object? passThrough = null)
    {
        return new ThumbnailOverlay((AnnotationThumbnail)target);
    }

    public IOverlay Show(object[] targets, object? passThrough = null)
    {
        throw new NotSupportedException();
    }

    private class ThumbnailOverlay : FrameworkElement, IOverlay
    {
        // Match the dimming used by Framework's OverlayWindow.
        private static readonly Brush MaskBrush = new SolidColorBrush(Color.FromArgb(0x77, 0, 0, 0));
        private readonly AnnotationThumbnail _thumbnail;
        private Geometry? _mask;

        public ThumbnailOverlay(AnnotationThumbnail thumbnail)
        {
            _thumbnail = thumbnail;
            IsHitTestVisible = false;
            Panel.SetZIndex(this, 1);
            _thumbnail.Children.Add(this);
            _thumbnail.PreviewChanged += OnPreviewChanged;
            UpdateMask();
        }

        public void Dispose()
        {
            _thumbnail.PreviewChanged -= OnPreviewChanged;
            _thumbnail.Children.Remove(this);
        }

        private void OnPreviewChanged(object? sender, EventArgs e)
        {
            UpdateMask();
        }

        private void UpdateMask()
        {
            _mask = _thumbnail.PreviewGeometry is { } geometry && !_thumbnail.PreviewBounds.IsEmpty
                ? Geometry.Combine(new RectangleGeometry(_thumbnail.PreviewBounds), geometry,
                    GeometryCombineMode.Exclude, null)
                : null;
            _mask?.Freeze();
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            if (_mask is not null)
            {
                drawingContext.DrawGeometry(MaskBrush, null, _mask);
            }
        }
    }
}