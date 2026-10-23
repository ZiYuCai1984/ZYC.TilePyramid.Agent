using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using ZYC.TilePyramid.Annotations;

namespace ZYC.Framework.Modules.TilePyramid.UI;

internal partial class TilePyramidDocumentView
{
    private (AnnotationItem Item, Point Position)? _canvasClick;

    private void MainView_OnAnnotationMouseDown(object sender, MouseButtonEventArgs e)
    {
        _canvasClick = null;
        if (!CanStartAnnotationEdit || e.ClickCount != 1 || Keyboard.Modifiers != ModifierKeys.None
            || e.RightButton != MouseButtonState.Released || e.MiddleButton != MouseButtonState.Released)
        {
            return;
        }

        var point = e.GetPosition(MainView);
        if (AnnotationLayer.HitItem(point) is { } item)
        {
            _canvasClick = (item, point);
        }
        // Leave the event unhandled so the viewport can capture the pointer and pan normally.
    }

    private void MainView_OnAnnotationMouseMove(object sender, MouseEventArgs e)
    {
        if (_canvasClick is { } click
            && (!CanStartAnnotationEdit || e.LeftButton != MouseButtonState.Pressed
                                        || e.RightButton != MouseButtonState.Released ||
                                        e.MiddleButton != MouseButtonState.Released
                                        || Keyboard.Modifiers != ModifierKeys.None ||
                                        IsCanvasDrag(click.Position, e.GetPosition(MainView))))
        {
            _canvasClick = null;
        }
    }

    private void MainView_OnAnnotationMouseUp(object sender, MouseButtonEventArgs e)
    {
        var click = _canvasClick;
        _canvasClick = null;
        var point = e.GetPosition(MainView);
        if (click is not { } target || !CanStartAnnotationEdit || Keyboard.Modifiers != ModifierKeys.None
            || e.RightButton != MouseButtonState.Released || e.MiddleButton != MouseButtonState.Released
            || IsCanvasDrag(target.Position, point) || !ReferenceEquals(AnnotationLayer.HitItem(point), target.Item))
        {
            return;
        }

        // Finish the viewport's mouse-up and release its capture before opening/focusing a list.
        Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
        {
            if (!CanStartAnnotationEdit || MainView.IsMouseCaptured)
            {
                return;
            }

            if (target.Item is Annotation annotation && CanFocusAnnotation(annotation))
            {
                ShowAnnotationInList(annotation);
            }
            else if (target.Item is RecognitionFeature feature)
            {
                SynchronizeRecognitionList();
                // IDs are scoped to batches; the rendered feature instance identifies the exact row.
                var row = _recognitionReviewItems.FirstOrDefault(item => ReferenceEquals(item.Feature, feature));
                if (row is not null && CanFocusRecognition(row))
                {
                    ShowRecognitionInList(row);
                }
            }
        }));
    }

    private static bool IsCanvasDrag(Point start, Point current)
    {
        return Math.Abs(current.X - start.X) >= SystemParameters.MinimumHorizontalDragDistance
               || Math.Abs(current.Y - start.Y) >= SystemParameters.MinimumVerticalDragDistance;
    }

    private void MainView_OnAnnotationMouseWheel(object sender, MouseWheelEventArgs e)
    {
        _canvasClick = null;
    }

    private void MainView_OnAnnotationCaptureLost(object sender, MouseEventArgs e)
    {
        _canvasClick = null;
    }
}