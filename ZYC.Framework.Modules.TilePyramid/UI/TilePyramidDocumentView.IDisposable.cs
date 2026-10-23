using System.Reactive.Disposables;

namespace ZYC.Framework.Modules.TilePyramid.UI;

internal partial class TilePyramidDocumentView
{
    private bool _disposed;

    private CompositeDisposable CompositeDisposable { get; } = new();

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _agentDebug?.Dispose();
        _labelImageCancellation.Cancel();
        _labelImageCancellation.Dispose();
        LabelImage = null;
        OnPropertyChanged(nameof(LabelImage));
        OnPropertyChanged(nameof(AnnotationSource));

        DisposeAnnotationEditing();
        DisposeRecognitionPanel();
        DisposeAgentTasks();

        AnnotationListView?.DetachFromSourceCollection();
        RecognitionListView?.DetachFromSourceCollection();
        CompositeDisposable.Dispose();

        Minimap.View = null;

        MainView.NavigationStateChanged -= MainView_OnNavigationStateChanged;
        MainView.MetricsChanged -= MainView_OnMetricsChanged;
        MainView.DiagnosticsChanged -= MainView_OnDiagnosticsChanged;
        MainView.FocalPlaneChanged -= MainView_OnFocalPlaneChanged;
        
        MainView.Source = null;
    }
}
