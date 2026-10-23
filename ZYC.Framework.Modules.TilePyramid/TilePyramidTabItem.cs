using System.ComponentModel;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Autofac;
using Microsoft.Extensions.Logging;
using ZYC.CoreToolkit.Extensions.Autofac.Attributes;
using ZYC.Framework.Abstractions;
using ZYC.Framework.Abstractions.Tab;
using ZYC.Framework.Core;
using ZYC.Framework.Core.Tab;
using ZYC.Framework.Modules.TilePyramid.Abstractions;
using ZYC.Framework.Modules.TilePyramid.UI;
using ZYC.TilePyramid.Annotations;
using ZYC.TilePyramid.Core.Sources;

namespace ZYC.Framework.Modules.TilePyramid;

[Register]
[ConstantsSource(typeof(TilePyramidModuleConstants))]
internal partial class TilePyramidTabItem : TabItemInstanceBase
{
    public TilePyramidTabItem(
        ILifetimeScope lifetimeScope,
        ILogger<TilePyramidTabItem> logger,
        MutableTabReference mutableTabReference,
        TilePyramidDocumentStateStore stateStore,
        ITabManager tabManager,
        TilePyramidAgentDocuments agentDocuments) : base(lifetimeScope, mutableTabReference)
    {
        StateStore = stateStore;
        TabManager = tabManager;
        AgentDocuments = agentDocuments;
        Route = TilePyramidDocumentRoute.Parse(mutableTabReference.Uri);
        mutableTabReference.Uri = Route.CreateViewUri(Route.InitialNavigation, Route.InitialFocalPlaneIndex);


        NavigationRequests
            .Throttle(TimeSpan.FromMilliseconds(512))
            .Where(isUserNavigation => isUserNavigation)
            .ObserveOnUI()
            // ReSharper disable once AsyncVoidLambda
            .Subscribe(async _ =>
            {
                try
                {
                    await UpdateNavigationUriAsync();
                }
                catch (Exception exception)
                {
                    logger.Error(exception);
                }
            }).DisposeWith(CompositeDisposable);


        NavigationRequests.DisposeWith(CompositeDisposable);
        FloatingPanelEvents.DisposeWith(CompositeDisposable);

        Title = TilePyramidModuleConstants.GetTabItemTitle(Route.Kind, Route.Source);
        Icon = TilePyramidSourceKindTools.GetIcon(Route.Kind);
    }

    public Subject<bool> NavigationRequests { get; } = new();

    public override string Title { get; }

    public override string Icon { get; }

    private TilePyramidDocumentStateStore StateStore { get; }
    private ITabManager TabManager { get; }
    private TilePyramidAgentDocuments AgentDocuments { get; }
    private MutableTabReference MutableTabReference => (MutableTabReference)TabReference;
    public TilePyramidDocumentRoute Route { get; }
    public TilePyramidDocumentState DocumentState { get; private set; } = new();
    public ITilePyramidSource Source { get; private set; } = null!;
    public AnnotationImage AnnotationImage { get; private set; } = null!;
    public TilePyramidAgentDocument? AgentDocument { get; private set; }
    public bool IsAgentAvailable => !_disposed && AgentDocument is not null;
    public TilePyramidDocumentView? CreatedView => _view as TilePyramidDocumentView;

    public override object View => _view ??= LifetimeScope.Resolve<TilePyramidDocumentView>(
        new TypedParameter(typeof(TilePyramidTabItem), this));


    public override async Task LoadAsync()
    {
        DocumentState = await StateStore.LoadAsync(Route);
        if (Route.InitialNavigation is { } navigation)
        {
            DocumentState.Navigation = navigation;
        }

        Source = await TilePyramidDocumentTools.OpenSourceAsync(Route);
        var focalPlaneIndex = Route.InitialFocalPlaneIndex ?? DocumentState.FocalPlaneIndex;
        DocumentState.FocalPlaneIndex = focalPlaneIndex is { } plane
                                        && (uint)plane < (uint)Source.Info.FocalPlanes.Count
            ? plane
            : Source.Info.DefaultFocalPlaneIndex;
        AnnotationImage = new AnnotationImage
        {
            ImageId = Route.StateKey,
            Width = Source.Info.Width,
            Height = Source.Info.Height,
            Annotations = DocumentState.Annotations,
            RecognitionBatches = DocumentState.RecognitionBatches
        };
        AgentDocument = new TilePyramidAgentDocument(Source, AnnotationImage);
        DocumentState.Annotations.Changed += OnAnnotationsChanged;
        DocumentState.RecognitionBatches.Changed += OnRecognitionBatchesChanged;
        DocumentState.AnnotationLayers.PropertyChanged += OnAnnotationLayersChanged;
        // ReSharper disable once SuspiciousTypeConversion.Global
        ((INotifyPropertyChanged)DocumentState).PropertyChanged += OnViewStateChanged;
        ObserveFloatingPanels();
        StateStore.Save(Route, DocumentState);
        AgentDocuments.Add(this);
    }

    public Uri CreateCurrentViewUri()
    {
        return Route.CreateViewUri(DocumentState.Navigation, DocumentState.FocalPlaneIndex);
    }

    public async Task UpdateNavigationUriAsync()
    {
        if (_disposed)
        {
            return;
        }

        var newUri = CreateCurrentViewUri();
        var oldUri = MutableTabReference.Uri;
        if (UriTools.Equals(oldUri, newUri))
        {
            return;
        }

        MutableTabReference.Uri = newUri;
        await TabManager.TabInternalNavigatingAsync(this, oldUri, newUri);
    }


    private void OnViewStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName)
            || e.PropertyName == nameof(TilePyramidDocumentState.FloatingPanels))
        {
            ObserveFloatingPanels();
        }

        StateStore.Save(Route, DocumentState);
    }

    private void OnAnnotationsChanged(object? sender, AnnotationItemsChangedEventArgs e)
    {
        StateStore.SaveAnnotationWorkspace(Route, DocumentState);
    }

    private void OnRecognitionBatchesChanged(object? sender, EventArgs e)
    {
        StateStore.SaveAnnotationWorkspace(Route, DocumentState);
    }

    private void OnAnnotationLayersChanged(object? sender, PropertyChangedEventArgs e)
    {
        StateStore.SaveAnnotationLayers(Route, DocumentState);
    }
}
