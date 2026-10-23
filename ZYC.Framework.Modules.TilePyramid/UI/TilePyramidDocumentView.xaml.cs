using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Input;
using Autofac;
using Microsoft.Extensions.Logging;
using ZYC.CoreToolkit.Extensions.Autofac.Attributes;
using ZYC.Framework.Abstractions;
using ZYC.Framework.Abstractions.Notification.Toast;
using ZYC.Framework.Abstractions.Overlay;
using ZYC.Framework.Modules.TilePyramid.Abstractions;
using ZYC.Framework.Modules.TilePyramid.Commands;
using ZYC.TilePyramid.Annotations;
using ZYC.TilePyramid.Core.Sources;
using ZYC.TilePyramid.Core.Viewports;
using ZYC.TilePyramid.Wpf;

namespace ZYC.Framework.Modules.TilePyramid.UI;

[Register]
internal partial class TilePyramidDocumentView : IDisposable, INotifyPropertyChanged
{
    private ICommand? _deleteAnnotationCommand;
    private ICommand? _focusAnnotationCommand;
    private ICommand? _setAnnotationsPanelPositionCommand;
    private ICommand? _setDebugPanelPositionCommand;
    private ICommand? _setDocumentInfoPositionCommand;
    private ICommand? _setMinimapPositionCommand;
    private ICommand? _setMinimapSizeCommand;
    private ICommand? _setRecognitionPanelPositionCommand;

    public TilePyramidDocumentView(
        ILifetimeScope lifetimeScope,
        IOverlayManager overlayManager,
        IDialogManager dialogManager,
        IToastManager toastManager,
        TilePyramidTabItem tabItem,
        TilePyramidAgentDocuments agentDocuments,
        TilePyramidAgentTasks agentTasks,
        ILogger<TilePyramidDocumentView> logger)
    {
        OverlayManager = overlayManager;
        LifetimeScope = lifetimeScope;
        DialogManager = dialogManager;
        ToastManager = toastManager;
        TabItem = tabItem;
        AgentDocuments = agentDocuments;
        AgentTasks = agentTasks;
        Logger = logger;

        InitializeDocumentInformation();
        InitializeComponent();
        InitializeAnnotationEditing();
        InitializeRecognitionPanel();
        InitializeAgentTasks();

        MainView.HttpClient = TilePyramidDocumentTools.SourceHttpClient;
        MainView.NavigationState = DocumentState.Navigation;
        MainView.Source = Source;
        MainView.FocalPlaneIndex = DocumentState.FocalPlaneIndex ?? Source.Info.DefaultFocalPlaneIndex;
        MainView.FocalPlaneChanged += MainView_OnFocalPlaneChanged;

        TabItem.UpdateDiagnostics(MainView.Diagnostics);

        UpdateFocalPlaneStatus();
        _ = LoadLabelImageAsync();
    }

    private ILifetimeScope LifetimeScope { get; }

    private IDialogManager DialogManager { get; }

    private IToastManager ToastManager { get; }

    private TilePyramidTabItem TabItem { get; }

    private TilePyramidAgentDocuments AgentDocuments { get; }
    private TilePyramidAgentTasks AgentTasks { get; }

    private ILogger<TilePyramidDocumentView> Logger { get; }

    public TilePyramidDocumentState DocumentState => TabItem.DocumentState;

    public bool IsViewInitialized => TabItem.Diagnostics?.IsViewInitialized == true;

    public ITilePyramidSource? AnnotationSource => _disposed ? null : Source;

    private ITilePyramidSource Source => TabItem.Source;

    public double ZoomFactor { get; private set; } = 1;

    public double RotationDegrees { get; private set; }

    public ICommand FocusAnnotationCommand => _focusAnnotationCommand ??=
        LifetimeScope.Resolve<FocusAnnotationCommand>(new TypedParameter(typeof(TilePyramidDocumentView), this));

    public ICommand DeleteAnnotationCommand => _deleteAnnotationCommand ??=
        LifetimeScope.Resolve<DeleteAnnotationCommand>(new TypedParameter(typeof(TilePyramidDocumentView), this));

    public ICommand SetMinimapPositionCommand => _setMinimapPositionCommand ??=
        LifetimeScope.Resolve<SetMinimapPositionCommand>(new TypedParameter(typeof(TilePyramidDocumentState),
            DocumentState));

    public ICommand SetMinimapSizeCommand => _setMinimapSizeCommand ??=
        LifetimeScope.Resolve<SetMinimapSizeCommand>(new TypedParameter(typeof(TilePyramidDocumentState),
            DocumentState));

    public ICommand SetDocumentInfoPositionCommand => _setDocumentInfoPositionCommand ??=
        LifetimeScope.Resolve<SetDocumentInfoPositionCommand>(new TypedParameter(typeof(TilePyramidDocumentState),
            DocumentState));

    public ICommand SetAnnotationsPanelPositionCommand => _setAnnotationsPanelPositionCommand ??=
        LifetimeScope.Resolve<SetAnnotationsPanelPositionCommand>(new TypedParameter(typeof(TilePyramidDocumentState),
            DocumentState));

    public ICommand SetRecognitionPanelPositionCommand => _setRecognitionPanelPositionCommand ??=
        LifetimeScope.Resolve<SetRecognitionPanelPositionCommand>(new TypedParameter(typeof(TilePyramidDocumentState),
            DocumentState));

    public ICommand SetDebugPanelPositionCommand => _setDebugPanelPositionCommand ??=
        LifetimeScope.Resolve<SetDebugPanelPositionCommand>(new TypedParameter(typeof(TilePyramidDocumentState),
            DocumentState));


    public event PropertyChangedEventHandler? PropertyChanged;

    public bool CanFocusAnnotation(Annotation annotation)
    {
        return CanStartAnnotationEdit && ReferenceEquals(DocumentState.Annotations.Find(annotation.Id), annotation);
    }

    public void FocusAnnotation(Annotation annotation)
    {
        if (CanFocusAnnotation(annotation))
        {
            var batch = annotation.Source is { } source ? DocumentState.RecognitionBatches.Find(source.BatchId) : null;
            FocusAnnotationRegion(annotation, batch?.FocalPlaneIndex ?? FocalPlaneIndex);
        }
    }

    private void FocusAnnotationRegion(AnnotationItem item, int focalPlaneIndex)
    {
        try
        {
            FocalPlaneIndex = focalPlaneIndex;
            var bounds = item.Bounds;
            MainView.FitTo(new ImageRect(bounds.X - bounds.Width / 2, bounds.Y - bounds.Height / 2,
                bounds.Width * 2, bounds.Height * 2));
        }
        catch (Exception exception)
        {
            Logger.Error(exception);
            ToastManager.PromptException(exception);
        }
    }

    private void MainView_OnContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        if (IsEditingAnnotation || !MainView.IsViewInitialized)
        {
            e.Handled = true;
        }
    }

    private void MainView_OnDiagnosticsChanged(object? sender, TilePyramidDiagnosticsChangedEventArgs e)
    {
        var wasInitialized = IsViewInitialized;
        TabItem.UpdateDiagnostics(e.Diagnostics);
        if (wasInitialized != IsViewInitialized)
        {
            OnPropertyChanged(nameof(IsViewInitialized));
        }
    }

    private void MainView_OnNavigationStateChanged(object? sender, TilePyramidNavigationStateChangedEventArgs e)
    {
        DocumentState.Navigation = e.NewState;

        //!WARNING When adding a new Reason, you must update this classification accordingly;
        // unlisted values ​​will silently return `false`, and—due to the processing order of `Throttle`
        // followed by `Where`—will cancel any pending navigation requests that have not yet been submitted.

        TabItem.NavigationRequests.OnNext(e.Reason is TilePyramidNavigationChangeReason.Pan
            or TilePyramidNavigationChangeReason.Zoom
            or TilePyramidNavigationChangeReason.Rotation
            or TilePyramidNavigationChangeReason.Fit
            or TilePyramidNavigationChangeReason.Center
            or TilePyramidNavigationChangeReason.Minimap);

        UpdateStatus();
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void UpdateStatus()
    {
        var metrics = MainView.Metrics;

        var navigation = MainView.NavigationState;
        if (navigation == null || metrics.ReferenceScale <= 0)
        {
            return;
        }

        UpdateZoomPresets(metrics.ReferenceScale, metrics.MaximumScale);

        ZoomFactor = navigation.Value.Scale / metrics.ReferenceScale;
        RotationDegrees = navigation.Value.RotationDegrees;


        OnPropertyChanged(nameof(ZoomFactor));
        OnPropertyChanged(nameof(RotationDegrees));
    }
}
