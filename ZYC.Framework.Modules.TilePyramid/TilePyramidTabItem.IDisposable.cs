using System.ComponentModel;
using System.Reactive.Disposables;
using ZYC.CoreToolkit;

namespace ZYC.Framework.Modules.TilePyramid;

internal partial class TilePyramidTabItem
{
    private bool _disposed;
    private CompositeDisposable CompositeDisposable { get; } = new();

    public override void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        AgentDocuments.Remove(this);

        // ReSharper disable once SuspiciousTypeConversion.Global
        ((INotifyPropertyChanged)DocumentState).PropertyChanged -= OnViewStateChanged;
        DocumentState.Annotations.Changed -= OnAnnotationsChanged;
        DocumentState.RecognitionBatches.Changed -= OnRecognitionBatchesChanged;
        DocumentState.AnnotationLayers.PropertyChanged -= OnAnnotationLayersChanged;
        try
        {
            CompositeDisposable.Dispose();

            _view?.TryDispose();
            StateStore.Flush();
        }
        finally
        {
            // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
            if (AgentDocument is not null)
            {
                AgentDocument.Dispose();
            }
            else
            {
                Source?.Dispose();
            }
        }
    }
}
