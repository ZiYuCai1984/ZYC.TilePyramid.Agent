using System.Reactive.Threading.Tasks;
using ZYC.TilePyramid.Annotations;
using ZYC.TilePyramid.Core.Sources;

namespace ZYC.Framework.Modules.TilePyramid.UI.Annotations;

/// <summary>Returns confirmed annotation properties from a cancellable, non-modal Framework dialog.</summary>
internal class AnnotationPropertiesRequest : IDisposable
{
    private readonly TaskCompletionSource<Annotation?> _completionSource =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private bool _disposed;

    public AnnotationPropertiesRequest(
        Annotation draft,
        string title,
        string confirmText,
        string cancelText,
        AnnotationLayerSettings layers,
        bool isCreating,
        ITilePyramidSource previewSource,
        int previewFocalPlaneIndex)
    {
        Draft = draft;
        Title = title;
        ConfirmText = confirmText;
        CancelText = cancelText;
        Layers = layers;
        IsCreating = isCreating;
        PreviewSource = previewSource;
        PreviewFocalPlaneIndex = previewFocalPlaneIndex;
        Result = _completionSource.Task.ToObservable();
    }

    public Annotation Draft { get; }
    public string Title { get; }
    public string ConfirmText { get; }
    public string CancelText { get; }
    public AnnotationLayerSettings Layers { get; }
    public bool IsCreating { get; }
    public ITilePyramidSource PreviewSource { get; }
    public int PreviewFocalPlaneIndex { get; }

    public IObservable<Annotation?> Result { get; }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        // Closing the window calls Complete synchronously. End the request before invoking that callback.
        _disposed = true;
        var closeWindow = CloseRequested;
        CloseRequested = null;
        _completionSource.TrySetResult(null);
        closeWindow?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? CloseRequested;

    public void Complete(Annotation? annotation)
    {
        if (_disposed)
        {
            return;
        }

        _completionSource.TrySetResult(annotation);
    }
}
