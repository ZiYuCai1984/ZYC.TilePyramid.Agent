using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Autofac;
using ZYC.Framework.Abstractions;
using ZYC.Framework.Core;
using ZYC.Framework.Core.Localizations;
using ZYC.TilePyramid.Annotations;
using ZYC.TilePyramid.Annotations.Wpf.Editing;
using ZYC.TilePyramid.Core.Sources;

namespace ZYC.Framework.Modules.TilePyramid.UI.Annotations;

/// <summary>One session owns creation, position edits and information edits until confirmation or cancellation.</summary>
internal class AnnotationEditingSession : IDisposable
{
    private readonly Subject<EditingAction> _actions = new();
    private readonly IDialogManager _dialogManager;
    private readonly AnnotationEditor _editor;
    private readonly Func<int> _getFocalPlaneIndex;
    private readonly Func<bool> _isContextValid;
    private readonly ITilePyramidSource _previewSource;
    private readonly SerialDisposable _propertiesRequestLifetime = new();
    private readonly Action<Exception> _reportError;
    private readonly IDisposable _stateSubscription;

    private readonly BehaviorSubject<AnnotationEditingState> _states =
        new(new AnnotationEditingState(AnnotationEditingPhase.Idle));

    private bool _disposed;

    public AnnotationEditingSession(
        ITilePyramidSource previewSource,
        Func<int> getFocalPlaneIndex,
        AnnotationEditor editor,
        IDialogManager dialogManager,
        Func<bool> isContextValid,
        Action<Exception> reportError)
    {
        _previewSource = previewSource;
        _getFocalPlaneIndex = getFocalPlaneIndex;
        _editor = editor;
        _dialogManager = dialogManager;
        _isContextValid = isContextValid;
        _reportError = reportError;

        var editorChanges = Observable.FromEventPattern(h => editor.Changed += h, h => editor.Changed -= h);
        var geometry = editorChanges.Where(_ => State.IsActive)
            .Select(_ => new EditingAction(ActionKind.GeometryChanged, Shape: editor.Preview,
                HasPendingEdit: editor.HasPendingEdit, Complete: editor.PendingCreation is not null,
                CanCompleteDraft: editor.CanCompleteCreation, IsDragging: editor.IsDragging));
        var invalidTarget = editorChanges
            .Where(_ => State.IsActive && State.Operation == AnnotationEditingOperation.Position
                                       && State.Phase != AnnotationEditingPhase.Committing
                                       && !ReferenceEquals(editor.EditingTarget, State.Target))
            .Select(_ => new EditingAction(ActionKind.Cancel));
        var invalidScene = Observable.FromEventPattern(h => editor.Scene.Changed += h, h => editor.Scene.Changed -= h)
            .Where(_ => State.IsActive && State.Phase != AnnotationEditingPhase.Committing && !CanContinue(State))
            .Select(_ => new EditingAction(ActionKind.Cancel));

        // Queue reentrant editor, scene and dialog notifications until the current transition completes.
        _stateSubscription = Observable.Merge(_actions, geometry, invalidTarget, invalidScene)
            .ObserveOn(CurrentThreadScheduler.Instance)
            .Scan(State, Reduce)
            .DistinctUntilChanged()
            .Subscribe(Apply);
    }

    public AnnotationEditingState State => _states.Value;
    public IObservable<AnnotationEditingState> States => _states.AsObservable();

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Cancel();
        _disposed = true;
        _stateSubscription.Dispose();
        _propertiesRequestLifetime.Dispose();
        _actions.Dispose();
        _states.OnCompleted();
        _states.Dispose();
    }

    public void StartCreation(AnnotationTool tool)
    {
        if (!_disposed && !State.IsActive && _isContextValid()
            && tool is AnnotationTool.Rectangle or AnnotationTool.Ellipse or AnnotationTool.Polygon
            && _editor.Scene.CanCreate(_editor.CreationLayerId))
        {
            _actions.OnNext(new EditingAction(ActionKind.Start, Tool: tool));
        }
    }

    public bool StartEdit(Annotation annotation, AnnotationEditingOperation operation)
    {
        if (_disposed || State.IsActive || !_isContextValid()
            || operation == AnnotationEditingOperation.Create || !CanEdit(annotation, operation))
        {
            return false;
        }

        _actions.OnNext(new EditingAction(ActionKind.Start, operation, Target: annotation));
        return true;
    }

    public void Confirm()
    {
        if (_disposed)
        {
            return;
        }

        if (State.CanCompleteDraft)
        {
            _editor.Complete();
        }
        else
        {
            _actions.OnNext(new EditingAction(ActionKind.Confirm));
        }
    }

    public void RemoveLastPoint()
    {
        if (!_disposed && State.Operation == AnnotationEditingOperation.Create
                       && State.Tool == AnnotationTool.Polygon && State.Phase == AnnotationEditingPhase.Drawing)
        {
            _editor.RemoveLastPoint();
        }
    }

    public void Cancel()
    {
        if (!_disposed)
        {
            _actions.OnNext(new EditingAction(ActionKind.Cancel));
        }
    }

    private bool CanEdit(Annotation annotation, AnnotationEditingOperation operation)
    {
        return ReferenceEquals(_editor.Scene.Image.Annotations.Find(annotation.Id), annotation)
               && !_editor.Scene.Layers.IsLocked(annotation.LayerId)
               && (operation == AnnotationEditingOperation.Information
                   || (_editor.Scene.IsVisible(annotation) && AnnotationGeometry.IsValid(annotation.Shape)));
    }

    private bool CanContinue(AnnotationEditingState state)
    {
        return state.Operation == AnnotationEditingOperation.Create
            ? _editor.Scene.CanCreate(_editor.CreationLayerId)
            : state.Target is { } target && CanEdit(target, state.Operation);
    }

    private static AnnotationEditingState Reduce(AnnotationEditingState state, EditingAction action)
    {
        return action.Kind switch
        {
            ActionKind.Start when !state.IsActive => new AnnotationEditingState(
                action.Operation switch
                {
                    AnnotationEditingOperation.Position => AnnotationEditingPhase.SelectionReady,
                    AnnotationEditingOperation.Information => AnnotationEditingPhase.EditingProperties,
                    _ => AnnotationEditingPhase.Ready
                }, action.Operation, action.Tool, action.Target, action.Target?.Shape),
            ActionKind.Cancel => new AnnotationEditingState(AnnotationEditingPhase.Idle),
            ActionKind.GeometryChanged when state.Operation == AnnotationEditingOperation.Position
                                            && state.Phase is AnnotationEditingPhase.Drawing
                                                or AnnotationEditingPhase.SelectionReady =>
                state with
                {
                    Phase = action.IsDragging ? AnnotationEditingPhase.Drawing : AnnotationEditingPhase.SelectionReady,
                    Shape = action.Shape
                },
            ActionKind.GeometryChanged when state.Operation == AnnotationEditingOperation.Create
                                            && state.Phase is AnnotationEditingPhase.Ready
                                                or AnnotationEditingPhase.Drawing
                                                or AnnotationEditingPhase.SelectionReady =>
                state with
                {
                    // Keep a selection returned from the dialog available until it is redrawn or reopened.
                    Phase = action.Complete
                        ? state.Phase == AnnotationEditingPhase.SelectionReady
                            ? AnnotationEditingPhase.SelectionReady
                            : AnnotationEditingPhase.EditingProperties
                        : action.HasPendingEdit ? AnnotationEditingPhase.Drawing : AnnotationEditingPhase.Ready,
                    Shape = action.Shape,
                    HasValidDraft = action.CanCompleteDraft
                },
            ActionKind.Confirm when state.CanConfirm => state with
            {
                Phase = state.Operation == AnnotationEditingOperation.Position
                    ? AnnotationEditingPhase.Committing
                    : AnnotationEditingPhase.EditingProperties
            },
            ActionKind.DialogClosed when state.Phase == AnnotationEditingPhase.EditingProperties =>
                action.Result is not null
                    ? state with { Phase = AnnotationEditingPhase.Committing, Result = action.Result }
                    : state.Operation == AnnotationEditingOperation.Create
                        ? state with { Phase = AnnotationEditingPhase.SelectionReady }
                        : new AnnotationEditingState(AnnotationEditingPhase.Idle),
            _ => state
        };
    }

    private void Apply(AnnotationEditingState state)
    {
        var previous = State;
        try
        {
            if (!previous.IsActive && state.IsActive)
            {
                if (state.Operation == AnnotationEditingOperation.Position)
                {
                    _editor.BeginShapeEdit(state.Target!);
                }
                else
                {
                    _editor.EndShapeEdit();
                    _editor.Tool = state.Tool;
                }
            }

            _states.OnNext(state);
            if (!state.IsActive || (previous.Phase == AnnotationEditingPhase.EditingProperties
                                    && state.Phase != AnnotationEditingPhase.EditingProperties))
            {
                _propertiesRequestLifetime.Disposable = null;
            }

            if (!state.IsActive)
            {
                _editor.EndShapeEdit();
            }
            else if (state.Phase == AnnotationEditingPhase.EditingProperties)
            {
                OpenProperties(state);
            }
            else if (state.Phase == AnnotationEditingPhase.Committing)
            {
                Commit(state);
            }
        }
        catch (Exception exception)
        {
            Cancel();
            _reportError(exception);
        }
    }

    private void OpenProperties(AnnotationEditingState state)
    {
        Annotation draft;
        string title;
        if (state.Target is { } target)
        {
            draft = new Annotation { Id = target.Id };
            draft.UpdateFrom(target);
            title = "Edit Annotation Information";
        }
        else
        {
            title = state.Tool switch
            {
                AnnotationTool.Rectangle => "Rectangle Annotation",
                AnnotationTool.Ellipse => "Ellipse Annotation",
                AnnotationTool.Polygon => "Polygon Annotation",
                _ => "Annotation"
            };
            draft = new Annotation
            {
                Shape = state.Shape!,
                LayerId = _editor.CreationLayerId,
                Label = DefaultNameGenerator.GetNext("Annotation",
                    _editor.Scene.Image.Annotations.Select(annotation => annotation.Label))
            };
        }

        var creating = state.Operation == AnnotationEditingOperation.Create;
        // Capture the current plane for each dialog, not when the document's session is initialized.
        var request = new AnnotationPropertiesRequest(draft, title,
            creating ? "Add" : "Save Information", creating ? "Back to Selection" : "Cancel", _editor.Scene.Layers, creating,
            _previewSource, _getFocalPlaneIndex());
        var resultSubscription = request.Result.ObserveOnUI().Take(1).Subscribe(annotation =>
            _actions.OnNext(new EditingAction(ActionKind.DialogClosed, Result: annotation)));
        _propertiesRequestLifetime.Disposable = new CompositeDisposable(resultSubscription, request);
        _dialogManager.Show<AnnotationPropertiesDialog>(
            new TypedParameter(typeof(AnnotationPropertiesRequest), request));
    }

    private void Commit(AnnotationEditingState state)
    {
        if (!_isContextValid() || !CanContinue(state))
        {
            throw new InvalidOperationException(L.T("The annotation, image or layer state has changed. Please edit again."));
        }

        var image = _editor.Scene.Image;
        switch (state.Operation)
        {
            case AnnotationEditingOperation.Create:
                var item = state.Result!;
                if (!ReferenceEquals(_editor.PendingCreation, state.Shape)
                    || _editor.Scene.Layers.Definitions.Find(item.LayerId) is null
                    || !_editor.Scene.CanEdit(item) || !ReferenceEquals(item.Shape, state.Shape)
                    || !AnnotationGeometry.IsWithin(item.Shape, image.Width, image.Height))
                {
                    throw new InvalidOperationException(L.T("The selection has changed. Please create the annotation again."));
                }

                image.Annotations.Add(item);
                break;
            case AnnotationEditingOperation.Position:
                if (!ReferenceEquals(_editor.EditingTarget, state.Target)
                    || !ReferenceEquals(_editor.EditedShape, state.Shape) || state.Shape is null
                    || !AnnotationGeometry.IsWithin(state.Shape, image.Width, image.Height))
                {
                    throw new InvalidOperationException(L.T("The annotation geometry has changed. Please edit again."));
                }

                state.Target!.Shape = state.Shape;
                break;
            case AnnotationEditingOperation.Information:
                if (_editor.Scene.Layers.Definitions.Find(state.Result!.LayerId) is null
                    || _editor.Scene.Layers.IsLocked(state.Result.LayerId))
                {
                    throw new InvalidOperationException(L.T("The target layer has been removed or locked. Please edit again."));
                }

                using (image.Annotations.BeginUpdate())
                {
                    state.Target!.Label = state.Result!.Label;
                    state.Target.Icon = state.Result.Icon;
                    state.Target.StrokeArgb = state.Result.StrokeArgb;
                    state.Target.Comments = state.Result.Comments;
                    state.Target.LayerId = state.Result.LayerId;
                }

                break;
        }

        Cancel();
    }

    private enum ActionKind
    {
        Start,
        GeometryChanged,
        Confirm,
        Cancel,
        DialogClosed
    }

    private record EditingAction(
        ActionKind Kind,
        AnnotationEditingOperation Operation = AnnotationEditingOperation.Create,
        AnnotationTool Tool = AnnotationTool.Navigate,
        Annotation? Target = null,
        AnnotationShape? Shape = null,
        bool HasPendingEdit = false,
        bool Complete = false,
        bool CanCompleteDraft = false,
        bool IsDragging = false,
        Annotation? Result = null);
}
