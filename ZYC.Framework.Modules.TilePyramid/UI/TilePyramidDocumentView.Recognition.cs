using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using Autofac;
using ZYC.Framework.Abstractions;
using ZYC.Framework.Modules.TilePyramid.Abstractions.Agent;
using ZYC.Framework.Modules.TilePyramid.Commands;
using ZYC.Framework.Modules.TilePyramid.UI.RecognitionReview;
using ZYC.TilePyramid.Annotations;

namespace ZYC.Framework.Modules.TilePyramid.UI;

internal partial class TilePyramidDocumentView
{
    private readonly ObservableCollection<RecognitionReviewItem> _recognitionReviewItems = new();
    private readonly Dictionary<(Guid BatchId, Guid Id), RecognitionReviewItem> _recognitionReviewLookup = new();
    private ReviewRecognitionCommand? _acceptRecognitionCommand;
    private ReviewRecognitionCommand? _createRecognitionAnnotationCommand;
    private FocusRecognitionCommand? _focusRecognitionCommand;
    private bool _recognitionActionActive;
    private ReviewDecision _recognitionListMode = ReviewDecision.Pending;
    private DispatcherOperation? _recognitionListRefresh;
    private ReviewRecognitionCommand? _rejectRecognitionCommand;
    private ReviewRecognitionCommand? _resetRecognitionReviewCommand;
    private RecognitionBatchItem? _selectedRecognitionBatch;

    public ListCollectionView? RecognitionListView { get; private set; }

    public ObservableCollection<RecognitionBatchItem> RecognitionBatches { get; } = [new(null)];

    public RecognitionBatchItem SelectedRecognitionBatch
    {
        get => _selectedRecognitionBatch ?? RecognitionBatches[0];
        set
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (value is null || ReferenceEquals(value, SelectedRecognitionBatch))
            {
                return;
            }

            _selectedRecognitionBatch = value;
            OnPropertyChanged();
            RefreshRecognitionListFilter();
        }
    }

    public bool IsAcceptedMode
    {
        get => _recognitionListMode == ReviewDecision.Accepted;
        set
        {
            if (value)
            {
                SetRecognitionListMode(ReviewDecision.Accepted);
            }
        }
    }

    public bool IsCandidatesMode
    {
        get => _recognitionListMode == ReviewDecision.Pending;
        set
        {
            if (value)
            {
                SetRecognitionListMode(ReviewDecision.Pending);
            }
        }
    }

    public bool IsRejectedMode
    {
        get => _recognitionListMode == ReviewDecision.Rejected;
        set
        {
            if (value)
            {
                SetRecognitionListMode(ReviewDecision.Rejected);
            }
        }
    }

    public int AcceptedReviewCount { get; private set; }

    public int PendingReviewCount { get; private set; }

    public int RejectedReviewCount { get; private set; }

    public int PendingRecognitionCount => DocumentState.RecognitionBatches.Sum(batch => batch.Candidates.Count());

    public ICommand FocusRecognitionCommand => _focusRecognitionCommand ??=
        LifetimeScope.Resolve<FocusRecognitionCommand>(new TypedParameter(typeof(TilePyramidDocumentView), this));

    public ICommand AcceptRecognitionCommand =>
        _acceptRecognitionCommand ??= CreateReviewCommand(RecognitionReviewAction.Accept);

    public ICommand RejectRecognitionCommand =>
        _rejectRecognitionCommand ??= CreateReviewCommand(RecognitionReviewAction.Reject);

    public ICommand ResetRecognitionReviewCommand =>
        _resetRecognitionReviewCommand ??= CreateReviewCommand(RecognitionReviewAction.ReturnToPending);

    public ICommand CreateRecognitionAnnotationCommand =>
        _createRecognitionAnnotationCommand ??= CreateReviewCommand(RecognitionReviewAction.CreateAnnotation);

    private bool CanWriteRecognition => CanStartAnnotationEdit
                                        && SharedLayerTabs.All(tab => tab.CreatedView?.IsEditingAnnotation != true);

    private ReviewRecognitionCommand CreateReviewCommand(RecognitionReviewAction action)
    {
        return LifetimeScope.Resolve<ReviewRecognitionCommand>(
            new TypedParameter(typeof(TilePyramidDocumentView), this),
            new TypedParameter(typeof(RecognitionReviewAction), action));
    }

    private void InitializeRecognitionPanel()
    {
        RecognitionPanelHost.IsVisibleChanged += RecognitionPanelHost_OnIsVisibleChanged;
        var view = RecognitionListView = new ListCollectionView(_recognitionReviewItems);
        view.Filter = MatchesRecognitionListFilter;
        view.LiveFilteringProperties.Add(nameof(RecognitionReviewItem.Decision));
        view.IsLiveFiltering = true;
        OnPropertyChanged(nameof(RecognitionListView));
        Observable.FromEventPattern<AnnotationItemsChangedEventArgs>(
                h => DocumentState.Annotations.Changed += h, h => DocumentState.Annotations.Changed -= h)
            .Subscribe(_ => ScheduleRecognitionListRefresh()).DisposeWith(CompositeDisposable);
        Observable.FromEventPattern(h => DocumentState.RecognitionBatches.Changed += h,
                h => DocumentState.RecognitionBatches.Changed -= h)
            .Subscribe(_ => ScheduleRecognitionListRefresh()).DisposeWith(CompositeDisposable);
        Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                h => DocumentState.AnnotationLayers.PropertyChanged += h,
                h => DocumentState.AnnotationLayers.PropertyChanged -= h)
            .Subscribe(_ => ScheduleRecognitionListRefresh()).DisposeWith(CompositeDisposable);
        Observable.FromEventPattern(h => AgentDocuments.AnnotationEditingChanged += h,
                h => AgentDocuments.AnnotationEditingChanged -= h)
            .Subscribe(_ => NotifyRecognitionCommands()).DisposeWith(CompositeDisposable);
        Observable.FromEventPattern(h => AgentDocuments.LayerContentsChanged += h,
                h => AgentDocuments.LayerContentsChanged -= h)
            .Subscribe(_ => NotifyRecognitionCommands()).DisposeWith(CompositeDisposable);
        SynchronizeRecognitionList();
    }

    private void ScheduleRecognitionListRefresh()
    {
        if (_disposed || _recognitionListRefresh?.Status == DispatcherOperationStatus.Pending)
        {
            return;
        }

        // Coalesce result and annotation association changes into one list update after a review.
        _recognitionListRefresh = Dispatcher.BeginInvoke(DispatcherPriority.DataBind, new Action(() =>
        {
            _recognitionListRefresh = null;
            if (!_disposed)
            {
                SynchronizeRecognitionList();
            }
        }));
    }

    private void SynchronizeRecognitionList()
    {
        var batches = DocumentState.RecognitionBatches;
        foreach (var option in RecognitionBatches
                     .Where(option => option.Batch is not null && !batches.Contains(option.Batch)).ToArray())
        {
            RecognitionBatches.Remove(option);
        }

        foreach (var batch in batches)
        {
            if (!RecognitionBatches.Any(option => ReferenceEquals(option.Batch, batch)))
            {
                RecognitionBatches.Add(new RecognitionBatchItem(batch));
            }
        }

        if (!RecognitionBatches.Contains(SelectedRecognitionBatch))
        {
            SelectedRecognitionBatch = RecognitionBatches[0];
        }

        var items = batches.SelectMany(batch =>
                batch.Features.Select(feature => (Model: feature, Batch: batch)))
            .ToArray();
        var keys = items.Select(item => (item.Batch.Id, item.Model.Id)).ToHashSet();
        foreach (var row in _recognitionReviewItems.Where(row => !keys.Contains(row.Key)).ToArray())
        {
            RemoveRecognitionReviewItem(row);
        }

        foreach (var (model, batch) in items)
        {
            var key = (batch.Id, model.Id);
            if (_recognitionReviewLookup.TryGetValue(key, out var row)
                && (!ReferenceEquals(row.Feature, model) || !ReferenceEquals(row.Batch, batch)))
            {
                RemoveRecognitionReviewItem(row);
                row = null;
            }

            if (row is null)
            {
                row = new RecognitionReviewItem(model, batch, DocumentState.Annotations, DocumentState.AnnotationLayers);
                _recognitionReviewLookup.Add(key, row);
                _recognitionReviewItems.Add(row);
            }
            else
            {
                row.RefreshReferences();
            }
        }

        UpdateRecognitionListStatistics();
        NotifyRecognitionCommands();
    }

    private void RemoveRecognitionReviewItem(RecognitionReviewItem row)
    {
        _recognitionReviewLookup.Remove(row.Key);
        _recognitionReviewItems.Remove(row);
        row.Dispose();
    }

    private bool InSelectedBatch(RecognitionReviewItem row)
    {
        return SelectedRecognitionBatch.Batch is not { } batch || row.BatchId == batch.Id;
    }

    private bool MatchesRecognitionListFilter(object item)
    {
        return item is RecognitionReviewItem row
               && row.Decision == _recognitionListMode && InSelectedBatch(row);
    }

    private void SetRecognitionListMode(ReviewDecision mode)
    {
        if (_recognitionListMode == mode)
        {
            return;
        }

        _recognitionListMode = mode;
        OnPropertyChanged(nameof(IsAcceptedMode));
        OnPropertyChanged(nameof(IsCandidatesMode));
        OnPropertyChanged(nameof(IsRejectedMode));
        RefreshRecognitionListFilter();
    }

    private void RefreshRecognitionListFilter()
    {
        RecognitionListView?.Refresh();
        UpdateRecognitionListStatistics();
        NotifyRecognitionCommands();
    }

    private void UpdateRecognitionListStatistics()
    {
        var rows = _recognitionReviewItems.Where(InSelectedBatch).ToArray();
        AcceptedReviewCount = rows.Count(row => row.Decision == ReviewDecision.Accepted);
        PendingReviewCount = rows.Count(row => row.Decision == ReviewDecision.Pending);
        RejectedReviewCount = rows.Count(row => row.Decision == ReviewDecision.Rejected);
        OnPropertyChanged(nameof(AcceptedReviewCount));
        OnPropertyChanged(nameof(PendingReviewCount));
        OnPropertyChanged(nameof(RejectedReviewCount));
        OnPropertyChanged(nameof(PendingRecognitionCount));
        UpdateRecognitionPreview();
    }

    private void RecognitionPanelHost_OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        UpdateRecognitionPreview();
    }

    private void UpdateRecognitionPreview()
    {
        if (_disposed || _annotationScene is null)
        {
            return;
        }

        //TODO-codex Restrict recognition overlays to the current focal plane and refresh them when it changes.
        var features = RecognitionPanelHost.IsVisible
            ? _recognitionReviewItems.Where(row => MatchesRecognitionListFilter(row)).Select(row => row.Feature)
            : Enumerable.Empty<RecognitionFeature>();
        _annotationScene.SetRecognitionPreview(features);
    }

    private bool IsCurrentRecognitionReviewItem(RecognitionReviewItem row)
    {
        return ReferenceEquals(DocumentState.RecognitionBatches.Find(row.BatchId), row.Batch)
               && ReferenceEquals(row.Batch.Features.Find(row.Feature.Id), row.Feature);
    }

    public bool CanReviewRecognition(RecognitionReviewItem row, RecognitionReviewAction action)
    {
        return CanWriteRecognition && IsCurrentRecognitionReviewItem(row) && !row.IsReviewLocked
               && (action switch
               {
                   RecognitionReviewAction.Accept => row.ShowAcceptAction,
                   RecognitionReviewAction.Reject => row.ShowRejectAction,
                   RecognitionReviewAction.ReturnToPending => row.ShowResetAction,
                   RecognitionReviewAction.CreateAnnotation => row.ShowCreateAnnotationAction,
                   _ => false
               });
    }

    public Task ReviewRecognitionAsync(RecognitionReviewItem row, RecognitionReviewAction action)
    {
        if (!CanReviewRecognition(row, action))
        {
            return Task.CompletedTask;
        }

        var documentId = TabItem.TabReference.Id;
        return RunRecognitionActionAsync(service => action switch
        {
            RecognitionReviewAction.Accept => service.AcceptRecognitionAsync(documentId, row.BatchId, row.Feature.Id),
            RecognitionReviewAction.Reject => service.RejectRecognitionAsync(documentId, row.BatchId, row.Feature.Id),
            RecognitionReviewAction.ReturnToPending => service.ResetRecognitionReviewAsync(documentId, row.BatchId, row.Feature.Id),
            RecognitionReviewAction.CreateAnnotation => service.CreateRecognitionAnnotationAsync(documentId, row.BatchId, row.Feature.Id),
            _ => throw new ArgumentOutOfRangeException(nameof(action))
        });
    }

    private async Task RunRecognitionActionAsync(Func<ITilePyramidAgentService, Task> action)
    {
        _recognitionActionActive = true;
        NotifyAnnotationAvailability();
        try
        {
            await action(LifetimeScope.Resolve<ITilePyramidAgentService>());
        }
        catch (Exception exception)
        {
            Logger.Error(exception);
            ToastManager.PromptException(exception);
        }
        finally
        {
            _recognitionActionActive = false;
            if (!_disposed)
            {
                ScheduleRecognitionListRefresh();
                NotifyAnnotationAvailability();
            }
        }
    }

    public bool CanFocusRecognition(RecognitionReviewItem row)
    {
        return CanStartAnnotationEdit && IsCurrentRecognitionReviewItem(row);
    }

    public void FocusRecognition(RecognitionReviewItem row)
    {
        if (CanFocusRecognition(row))
        {
            FocusAnnotationRegion(row.Feature, row.Batch.FocalPlaneIndex);
        }
    }

    private void NotifyRecognitionCommands()
    {
        _acceptRecognitionCommand?.RaiseCanExecuteChanged();
        _rejectRecognitionCommand?.RaiseCanExecuteChanged();
        _resetRecognitionReviewCommand?.RaiseCanExecuteChanged();
        _createRecognitionAnnotationCommand?.RaiseCanExecuteChanged();
        _focusRecognitionCommand?.RaiseCanExecuteChanged();
        _navigateToRecognitionCommand?.RaiseCanExecuteChanged();
        _navigateToAnnotationCommand?.RaiseCanExecuteChanged();
    }

    private void DisposeRecognitionPanel()
    {
        RecognitionPanelHost.IsVisibleChanged -= RecognitionPanelHost_OnIsVisibleChanged;
        _recognitionListRefresh?.Abort();
        foreach (var row in _recognitionReviewItems)
        {
            row.Dispose();
        }

        _recognitionReviewLookup.Clear();
    }
}
