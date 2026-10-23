using System.Windows;
using System.Windows.Media;
using ZYC.TilePyramid.Annotations;
using ZYC.TilePyramid.Annotations.Wpf;
using ZYC.TilePyramid.Wpf;

namespace ZYC.Framework.Modules.TilePyramid.UI.Annotations;

/// <summary>Dims the editing surface outside the selection in the same window as the preview.</summary>
internal class AnnotationSelectionMask : FrameworkElement
{
    // Match Framework's OverlayWindow mask outside the editing surface.
    private static readonly Brush MaskBrush = new SolidColorBrush(Color.FromArgb(0x77, 0, 0, 0));
    private Geometry? _selectionGeometry;

    public AnnotationSelectionMask()
    {
        IsHitTestVisible = false;
        ClipToBounds = true;
    }

    public void Update(TilePyramidView view, AnnotationShape? shape)
    {
        _selectionGeometry = null;
        if (!view.IsViewInitialized || shape is null || !AnnotationGeometry.IsValid(shape))
        {
            InvalidateVisual();
            return;
        }

        // Share the exact geometry (including ellipse curves and polygon fill rule) used by the renderer.
        var geometry = AnnotationDrawingItem.CreateGeometry(shape).Clone();
        var transform = new Matrix(1, 0, 0, 1, shape.Bounds.X, shape.Bounds.Y);
        transform.Append(view.ImageToViewTransform.Value);
        geometry.Transform = new MatrixTransform(transform);
        geometry.Freeze();
        _selectionGeometry = geometry;
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        // Use the current surface size so resizing never leaves an uncovered strip.
        var surface = new RectangleGeometry(new Rect(RenderSize));
        Geometry mask = _selectionGeometry is null
            ? surface
            : new CombinedGeometry(GeometryCombineMode.Exclude, surface, _selectionGeometry);
        drawingContext.DrawGeometry(MaskBrush, null, mask);
    }
}