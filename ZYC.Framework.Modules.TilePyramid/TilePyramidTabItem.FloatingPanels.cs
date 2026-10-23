using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace ZYC.Framework.Modules.TilePyramid;

internal partial class TilePyramidTabItem
{
    private SerialDisposable FloatingPanelEvents { get; } = new();

    private void ObserveFloatingPanels()
    {
        var panels = DocumentState.FloatingPanels;
        FloatingPanelEvents.Disposable = Observable.FromEventPattern(
                handler => panels.Changed += handler,
                handler => panels.Changed -= handler)
            .Subscribe(_ => StateStore.Save(Route, DocumentState));
    }
}
