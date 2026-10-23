using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Autofac;
using ZYC.Framework.Abstractions;
using ZYC.Framework.Abstractions.Overlay;
using ZYC.Framework.Core.Localizations;
using ZYC.Framework.Modules.TilePyramid.Commands;
using ZYC.Framework.Modules.TilePyramid.UI.Annotations;
using ZYC.TilePyramid.Annotations;
using ZYC.TilePyramid.Annotations.Wpf;
using ZYC.TilePyramid.Annotations.Wpf.Editing;
using ZYC.TilePyramid.Core.Viewports;
using ZYC.TilePyramid.Wpf;

namespace ZYC.Framework.Modules.TilePyramid.UI;

internal partial class TilePyramidDocumentView
{
    private readonly RectangleGeometry _annotationEditingClip = new(Rect.Empty);
    private readonly SerialDisposable _ownerEvents = new();

    private AnnotationEditor? _annotationEditor;

    private AnnotationScene? _annotationScene;
    private ICommand? _editAnnotationInformationCommand;
    private ICommand? _editAnnotationPositionCommand;

    private IOverlay? _editingOverlay;

    private AnnotationEditingSession? _editingSession;

    private AnnotationEditingState _editingState = new(AnnotationEditingPhase.Idle);

    private IDisposable? _restoreNavigation;
    private bool _suppressAnnotationCancelMouseUp;

    public ICommand EditAnnotationInformationCommand => _editAnnotationInformationCommand ??=
        LifetimeScope.Resolve<EditAnnotationInformationCommand>(
            new TypedParameter(typeof(TilePyramidDocumentView), this));

    public ICommand EditAnnotationPositionCommand =>
        _editAnnotationPositionCommand ??=
            LifetimeScope.Resolve<EditAnnotationPositionCommand>(
                new TypedParameter(typeof(TilePyramidDocumentView), this));

    public Subject<AnnotationTool> AnnotationCreationRequests { get; } = new();

    private IOverlayManager OverlayManager { get; }

    public bool IsEditingAnnotation => _editingState.IsActive;
    public bool CanStartAnnotationEdit => !IsEditingAnnotation && !_recognitionActionActive && IsAnnotationContextValid();
    public bool CanConfirmAnnotation => _editingState.CanConfirm || _editingState.CanCompleteDraft;

    public bool ShowAnnotationConfirmation => _editingState.Phase is AnnotationEditingPhase.Ready
                                                  or AnnotationEditingPhase.Drawing
                                                  or AnnotationEditingPhase.SelectionReady
                                              && (_editingState.Operation == AnnotationEditingOperation.Position
                                                  || _editingState.Tool == AnnotationTool.Polygon
                                                  || _editingState.CanConfirm);

    public string AnnotationConfirmationText => _editingState.Operation switch
    {
        AnnotationEditingOperation.Position => "Save Geometry",
        AnnotationEditingOperation.Information => "Save Information",
        _ when _editingState.Tool == AnnotationTool.Polygon
               && _editingState.Phase is AnnotationEditingPhase.Ready or AnnotationEditingPhase.Drawing =>
            "Complete Polygon",
        _ => "Edit Annotation Information"
    };

    public bool CanCreateAnnotation => CanStartAnnotationEdit
                                       && _annotationScene?.CanCreate(_annotationEditor!.CreationLayerId) == true;

    public string AnnotationEditingHint => _editingState.Operation switch
    {
        AnnotationEditingOperation.Position =>
            "Edit geometry: drag the annotation to move it or drag handles to reshape it; Enter to save; Esc to cancel",
        AnnotationEditingOperation.Information =>
            "Edit information: change the name, layer, notes and color in the dialog",
        _ => _editingState.Phase switch
        {
            AnnotationEditingPhase.Ready when _editingState.Tool == AnnotationTool.Polygon =>
                "Click to add a vertex; Esc to cancel",
            AnnotationEditingPhase.Drawing when _editingState.Tool == AnnotationTool.Polygon =>
                _editingState.CanCompleteDraft
                    ? "Enter or double-click to close; Backspace to undo a vertex; Esc to cancel"
                    : "Add at least three non-collinear vertices; Backspace to undo; Esc to cancel",
            AnnotationEditingPhase.SelectionReady when _editingState.Tool == AnnotationTool.Polygon =>
                "Enter to edit information; click to redraw; Esc to cancel",
            AnnotationEditingPhase.Ready => "Drag to select; Esc to cancel",
            AnnotationEditingPhase.Drawing => "Release to edit annotation information; Esc to cancel",
            AnnotationEditingPhase.SelectionReady => "Enter to edit information; drag again to redraw; Esc to cancel",
            AnnotationEditingPhase.EditingProperties => "Edit properties in the dialog",
            _ => ""
        }
    };

    public void StartAnnotationEdit(Annotation annotation, AnnotationEditingOperation operation)
    {
        if (CanStartAnnotationEdit && _editingSession?.StartEdit(annotation, operation) == false)
        {
            ToastManager.PromptException(new InvalidOperationException(
                operation == AnnotationEditingOperation.Position
                    ? L.T(
                        "Cannot edit geometry: the annotation must be visible, its shape valid, and its layer unlocked.")
                    : L.T("Cannot edit information: the annotation has been removed or its layer is locked.")));
        }
    }

    private void NotifyAnnotationAvailability()
    {
        OnPropertyChanged(nameof(CanStartAnnotationEdit));
        OnPropertyChanged(nameof(CanCreateAnnotation));

        AnnotationToolCommand.RaiseCanExecuteChanged();
        (_deleteAnnotationCommand as DeleteAnnotationCommand)?.RaiseCanExecuteChanged();
        (_editAnnotationInformationCommand as EditAnnotationInformationCommand)?.RaiseCanExecuteChanged();
        (_editAnnotationPositionCommand as EditAnnotationPositionCommand)?.RaiseCanExecuteChanged();
        (_focusAnnotationCommand as FocusAnnotationCommand)?.RaiseCanExecuteChanged();
        NotifyAnnotationLayerCommands();
        NotifyRecognitionCommands();
    }

    public bool CanEditAnnotation(Annotation annotation, AnnotationEditingOperation operation)
    {
        return CanDeleteAnnotation(annotation)
               && (operation == AnnotationEditingOperation.Information
                   || (_annotationScene!.IsVisible(annotation) && AnnotationGeometry.IsValid(annotation.Shape)));
    }

    public bool CanDeleteAnnotation(Annotation annotation)
    {
        return CanStartAnnotationEdit
               // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
               && annotation is not null
               && ReferenceEquals(DocumentState.Annotations.Find(annotation.Id), annotation)
               && !DocumentState.AnnotationLayers.IsLocked(annotation.LayerId);
    }

    public void DeleteAnnotation(Annotation annotation)
    {
        if (CanDeleteAnnotation(annotation))
        {
            TabItem.AgentDocument!.Workspace.RemoveAnnotation(TabItem.AnnotationImage.ImageId, annotation.Id);
        }
    }

    private bool IsAnnotationContextValid()
    {
        return !_disposed && IsLoaded && IsVisible
               && MainView.IsViewInitialized && ReferenceEquals(MainView.Source, Source);
    }

    private void InitializeAnnotationEditing()
    {
        // Keep both layers in MainView coordinates, clipping only their editable area.
        AnnotationInteraction.Clip = SelectionMask.Clip = _annotationEditingClip;
        _annotationScene = new AnnotationScene(TabItem.AnnotationImage, DocumentState.AnnotationLayers);
        _annotationEditor = new AnnotationEditor(_annotationScene) { DeferCreationCommit = true };

        // Scene owns the display projection, Editor handles geometry drafts, and Session coordinates commit/cancel.
        InitializeAnnotationLayers();
        AnnotationLayer.Scene = MinimapAnnotationLayer.Scene = _annotationScene;
        AnnotationInteraction.Editor = _annotationEditor;

        _editingSession = new AnnotationEditingSession(Source, () => FocalPlaneIndex,
            _annotationEditor, DialogManager, IsAnnotationContextValid,
            exception =>
            {
                Logger.Error(exception);
                ToastManager.PromptException(exception);
            });
        _editingSession.States.Subscribe(state =>
        {
            try
            {
                UpdateAnnotationEditing(state);
            }
            catch (Exception exception)
            {
                // Keep the observer alive so the cancellation transition can restore the view,
                // including when creating the native Framework overlay fails.
                _editingSession.Cancel();
                Logger.Error(exception);
                ToastManager.PromptException(exception);
            }
        }).DisposeWith(CompositeDisposable);
        AnnotationCreationRequests.Where(_ => CanCreateAnnotation)
            .Subscribe(tool => _editingSession.StartCreation(tool)).DisposeWith(CompositeDisposable);
        AnnotationCreationRequests.DisposeWith(CompositeDisposable);
        _ownerEvents.DisposeWith(CompositeDisposable);

        Observable.FromEventPattern(h => MainView.ViewTransformChanged += h, h => MainView.ViewTransformChanged -= h)
            .Subscribe(_ => SelectionMask.Update(MainView, _editingState.Shape)).DisposeWith(CompositeDisposable);
        Observable.FromEventPattern(h => MainView.ViewInitialized += h, h => MainView.ViewInitialized -= h)
            .Subscribe(_ => NotifyAnnotationAvailability()).DisposeWith(CompositeDisposable);
        Observable.FromEventPattern(h => MainView.SourceChanged += h, h => MainView.SourceChanged -= h)
            .Subscribe(_ =>
            {
                _editingSession.Cancel();
                NotifyAnnotationAvailability();
            }).DisposeWith(CompositeDisposable);
        Observable.FromEventPattern(h => _annotationScene.Changed += h, h => _annotationScene.Changed -= h)
            .Subscribe(_ =>
            {
                RefreshAnnotationLayers();
                NotifyAnnotationAvailability();
            }).DisposeWith(CompositeDisposable);

        Loaded += AnnotationEditing_OnLoaded;
        Unloaded += AnnotationEditing_OnUnloaded;
        LayoutUpdated += AnnotationEditing_OnLayoutUpdated;
        IsVisibleChanged += AnnotationEditing_OnIsVisibleChanged;
        PreviewKeyDown += AnnotationEditing_OnPreviewKeyDown;
        PreviewMouseDown += AnnotationEditing_OnPreviewMouseDown;
        PreviewMouseUp += AnnotationEditing_OnPreviewMouseUp;
        PreviewMouseWheel += AnnotationEditing_OnPreviewMouseWheel;
    }

    private void AnnotationEditing_OnLayoutUpdated(object? sender, EventArgs e)
    {
        if (_disposed || !IsLoaded || !MainView.IsArrangeValid || !EditingSurface.IsArrangeValid)
        {
            return;
        }

        // Derive the opening from layout so toolbar size and position changes stay aligned
        // with the native overlay without shifting the annotation coordinate system.
        var bounds = EditingSurface.TransformToVisual(MainView)
            .TransformBounds(new Rect(EditingSurface.RenderSize));
        bounds.Intersect(new Rect(MainView.RenderSize));
        if (_annotationEditingClip.Rect != bounds)
        {
            _annotationEditingClip.Rect = bounds;
        }
    }

    private void UpdateAnnotationEditing(AnnotationEditingState state)
    {
        var previous = _editingState;
        _editingState = state;
        if (!previous.IsActive && state.IsActive)
        {
            BeginAnnotationEditing();
        }

        if (previous.IsActive && !state.IsActive)
        {
            EndAnnotationEditing();
        }

        EditingInputBlocker.Visibility = state.Phase is AnnotationEditingPhase.EditingProperties
            or AnnotationEditingPhase.Committing
            ? Visibility.Visible
            : Visibility.Collapsed;
        SelectionMask.Update(MainView, state.Shape);
        if (previous.Phase == AnnotationEditingPhase.EditingProperties && state.CanConfirm &&
            IsAnnotationContextValid())
        {
            AnnotationInteraction.Focus();
        }

        OnPropertyChanged(nameof(IsEditingAnnotation));
        if (previous.IsActive != state.IsActive) AgentDocuments.NotifyAnnotationEditingChanged();
        OnPropertyChanged(nameof(CanConfirmAnnotation));
        OnPropertyChanged(nameof(ShowAnnotationConfirmation));

        NotifyAnnotationAvailability();

        OnPropertyChanged(nameof(AnnotationEditingHint));
        OnPropertyChanged(nameof(AnnotationConfirmationText));
    }

    private void BeginAnnotationEditing()
    {
        if (_editingState.Target is { } target && AnnotationGeometry.IsValid(target.Shape))
        {
            var bounds = target.Bounds;
            MainView.FitTo(new ImageRect(bounds.X - bounds.Width / 2, bounds.Y - bounds.Height / 2,
                bounds.Width * 2, bounds.Height * 2));
        }

        var pan = MainView.IsPanEnabled;
        var zoom = MainView.IsZoomEnabled;
        var rotation = MainView.IsRotationEnabled;
        var minimapNavigation = Minimap.IsNavigationEnabled;
        var topToolbarEnabled = TopToolbarView.IsEnabled;
        var bottomToolbarEnabled = BottomToolbarView.IsEnabled;
        var focused = Keyboard.FocusedElement;
        _restoreNavigation = Disposable.Create(() =>
        {
            MainView.SetCurrentValue(TilePyramidView.IsPanEnabledProperty, pan);
            MainView.SetCurrentValue(TilePyramidView.IsZoomEnabledProperty, zoom);
            MainView.SetCurrentValue(TilePyramidView.IsRotationEnabledProperty, rotation);
            Minimap.SetCurrentValue(TilePyramidMinimap.IsNavigationEnabledProperty, minimapNavigation);
            TopToolbarView.SetCurrentValue(IsEnabledProperty, topToolbarEnabled);
            BottomToolbarView.SetCurrentValue(IsEnabledProperty, bottomToolbarEnabled);
            if (!_disposed && IsLoaded && IsVisible && focused is UIElement { IsVisible: true } element)
            {
                element.Focus();
            }
        });

        MainView.SetCurrentValue(TilePyramidView.IsPanEnabledProperty, false);
        MainView.SetCurrentValue(TilePyramidView.IsZoomEnabledProperty, false);
        MainView.SetCurrentValue(TilePyramidView.IsRotationEnabledProperty, false);
        Minimap.SetCurrentValue(TilePyramidMinimap.IsNavigationEnabledProperty, false);
        TopToolbarView.SetCurrentValue(IsEnabledProperty, false);
        BottomToolbarView.SetCurrentValue(IsEnabledProperty, false);

        SelectionMask.Update(MainView, _editingState.Shape);

        // The native overlay covers the toolbars; the clipped canvas mask owns the selection hole.
        _editingOverlay = OverlayManager.Show(EditingSurface);
        AnnotationInteraction.Focus();
    }

    private void EndAnnotationEditing()
    {
        var overlay = _editingOverlay;
        _editingOverlay = null;
        try
        {
            overlay?.Dispose();
        }
        finally
        {
            AnnotationInteraction.CancelInteraction();
            SelectionMask.Update(MainView, null);
            var restore = _restoreNavigation;
            _restoreNavigation = null;
            restore?.Dispose();
        }
    }

    private void ConfirmAnnotation_OnClick(object sender, RoutedEventArgs e)
    {
        _editingSession?.Confirm();
    }

    private void CancelAnnotation_OnClick(object sender, RoutedEventArgs e)
    {
        _editingSession?.Cancel();
    }

    private void AnnotationEditing_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!IsEditingAnnotation)
        {
            return;
        }

        if (_editingState.Phase is AnnotationEditingPhase.EditingProperties or AnnotationEditingPhase.Committing)
        {
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape)
        {
            _editingSession?.Cancel();
        }
        else if (e.Key == Key.Enter)
        {
            if (!e.IsRepeat)
            {
                _editingSession?.Confirm();
            }
        }
        else if (e.Key == Key.Back)
        {
            _editingSession?.RemoveLastPoint();
        }
        else if (e.Key != Key.Delete)
        {
            return;
        }

        e.Handled = true;
    }

    private void AnnotationEditing_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Right)
        {
            _suppressAnnotationCancelMouseUp = IsEditingAnnotation;
        }

        if (!IsEditingAnnotation)
        {
            return;
        }

        if (e.ChangedButton == MouseButton.Right)
        {
            _editingSession?.Cancel();
        }
        else if (e.ChangedButton != MouseButton.Middle)
        {
            return;
        }

        e.Handled = true;
    }

    private void AnnotationEditing_OnPreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Right && _suppressAnnotationCancelMouseUp)
        {
            // Cancellation ends editing on mouse-down; consume this release before WPF opens a context menu.
            _suppressAnnotationCancelMouseUp = false;
            e.Handled = true;
        }
    }

    private void AnnotationEditing_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (IsEditingAnnotation)
        {
            e.Handled = true;
        }
    }

    private void AnnotationEditing_OnLoaded(object sender, RoutedEventArgs e)
    {
        NotifyAnnotationAvailability();
        if (Window.GetWindow(this) is { } owner)
        {
            _ownerEvents.Disposable = Observable
                .FromEventPattern(h => owner.Deactivated += h, h => owner.Deactivated -= h)
                .Where(_ => IsEditingAnnotation && _annotationEditor?.IsDragging == true)
                .Subscribe(_ => AnnotationInteraction.CancelInteraction());
        }
    }

    private void AnnotationEditing_OnUnloaded(object sender, RoutedEventArgs e)
    {
        _suppressAnnotationCancelMouseUp = false;
        _editingSession?.Cancel();
        _ownerEvents.Disposable = null;
    }

    private void AnnotationEditing_OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (!IsVisible)
        {
            _editingSession?.Cancel();
        }

        NotifyAnnotationAvailability();
    }

    private void DisposeAnnotationEditing()
    {
        Loaded -= AnnotationEditing_OnLoaded;
        Unloaded -= AnnotationEditing_OnUnloaded;
        LayoutUpdated -= AnnotationEditing_OnLayoutUpdated;
        IsVisibleChanged -= AnnotationEditing_OnIsVisibleChanged;
        PreviewKeyDown -= AnnotationEditing_OnPreviewKeyDown;
        PreviewMouseDown -= AnnotationEditing_OnPreviewMouseDown;
        PreviewMouseUp -= AnnotationEditing_OnPreviewMouseUp;
        PreviewMouseWheel -= AnnotationEditing_OnPreviewMouseWheel;
        _editingSession?.Dispose();
        _editingSession = null;
        EndAnnotationEditing();
        AnnotationInteraction.Editor = null;
        AnnotationLayer.Scene = MinimapAnnotationLayer.Scene = null;
        _annotationEditor?.Dispose();
        _annotationScene?.Dispose();
    }
}
