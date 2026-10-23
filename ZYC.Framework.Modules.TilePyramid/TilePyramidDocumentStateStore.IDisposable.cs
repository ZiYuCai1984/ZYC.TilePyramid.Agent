using System.Reactive.Disposables;

namespace ZYC.Framework.Modules.TilePyramid;

internal partial class TilePyramidDocumentStateStore
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

        CompositeDisposable.Dispose();
        AppContext.Exit -= OnAppExit;
    }
}