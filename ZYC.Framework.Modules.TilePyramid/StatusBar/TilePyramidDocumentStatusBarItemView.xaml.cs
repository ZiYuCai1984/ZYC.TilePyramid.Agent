using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using ZYC.CoreToolkit.Extensions.Autofac.Attributes;
using ZYC.Framework.Abstractions;
using ZYC.Framework.Abstractions.Event;
using ZYC.Framework.Abstractions.Tab;
using ZYC.Framework.Abstractions.Workspace;
using ZYC.Framework.Modules.TilePyramid.Abstractions;

namespace ZYC.Framework.Modules.TilePyramid.StatusBar;

[RegisterSingleInstance]
internal partial class TilePyramidDocumentStatusBarItemView : INotifyPropertyChanged, IDisposable
{
    public TilePyramidDocumentStatusBarItemView(
        TilePyramidConfig tilePyramidConfig,
        ITabManager tabManager,
        IParallelWorkspaceManager parallelWorkspaceManager,
        IEventAggregator eventAggregator)
    {
        TilePyramidConfig = tilePyramidConfig;
        TabManager = tabManager;
        ParallelWorkspaceManager = parallelWorkspaceManager;

        InitializeComponent();

        eventAggregator.Subscribe<FocusedTabItemChangedEvent>(_ => UpdateActiveDocument(), true)
            .DisposeWith(CompositeDisposable);
        eventAggregator.Subscribe<WorkspaceFocusChangedEvent>(_ => UpdateActiveDocument(), true)
            .DisposeWith(CompositeDisposable);
        UpdateActiveDocument();
    }

    private CompositeDisposable CompositeDisposable { get; } = new();

    public TilePyramidConfig TilePyramidConfig { get; }

    private ITabManager TabManager { get; }

    private IParallelWorkspaceManager ParallelWorkspaceManager { get; }

    public TilePyramidTabItem? ActiveDocument { get; private set; }

    public void Dispose()
    {
        CompositeDisposable.Dispose();
        ActiveDocument = null;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ActiveDocument)));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void UpdateActiveDocument()
    {
        var workspace = ParallelWorkspaceManager.GetFocusedWorkspace();
        var document = TabManager.GetFocusedTabItemInstance(workspace.Id) as TilePyramidTabItem;
        if (ReferenceEquals(ActiveDocument, document))
        {
            return;
        }

        ActiveDocument = document;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ActiveDocument)));
    }
}