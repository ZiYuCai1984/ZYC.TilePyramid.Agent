using System.Collections.ObjectModel;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Windows.Data;
using System.Windows.Input;
using Autofac;
using ZYC.Framework.Core.Localizations;
using ZYC.Framework.Modules.TilePyramid.Commands;
using ZYC.Framework.Modules.TilePyramid.UI.Annotations;
using ZYC.TilePyramid.Annotations;

namespace ZYC.Framework.Modules.TilePyramid.UI;

internal partial class TilePyramidDocumentView
{
    private readonly PropertyGroupDescription _annotationLayerGrouping = new(nameof(Annotation.LayerId));

    private ICommand? _deleteAnnotationLayerCommand;

    private ICommand? _editAnnotationLayerCommand;

    private ICommand? _moveDownAnnotationLayerCommand;

    private ICommand? _moveUpAnnotationLayerCommand;

    private ICommand? _newAnnotationLayerDialogCommand;

    private bool _refreshingAnnotationLayers;
    private AnnotationLayerItem? _selectedAnnotationLayer;

    private ICommand? _setAnnotationLayerLockStateCommand;

    private ICommand? _setAnnotationLayerVisibleStateCommand;

    private SetCurrentAnnotationLayerCommand? _setCurrentAnnotationLayerCommand;

    public SetCurrentAnnotationLayerCommand SetCurrentAnnotationLayerCommand => _setCurrentAnnotationLayerCommand ??=
        LifetimeScope.Resolve<SetCurrentAnnotationLayerCommand>(
            new TypedParameter(typeof(TilePyramidDocumentView), this));


    public ICommand NewAnnotationLayerDialogCommand => _newAnnotationLayerDialogCommand ??=
        LifetimeScope.Resolve<NewAnnotationLayerDialogCommand>(
            new TypedParameter(typeof(TilePyramidDocumentView), this));

    public ICommand DeleteAnnotationLayerCommand => _deleteAnnotationLayerCommand ??=
        LifetimeScope.Resolve<DeleteAnnotationLayerCommand>(
            new TypedParameter(typeof(TilePyramidDocumentView), this));

    public ICommand EditAnnotationLayerCommand => _editAnnotationLayerCommand ??=
        LifetimeScope.Resolve<EditAnnotationLayerCommand>(
            new TypedParameter(typeof(TilePyramidDocumentView), this));

    public ICommand MoveDownAnnotationLayerCommand => _moveDownAnnotationLayerCommand ??=
        LifetimeScope.Resolve<MoveDownAnnotationLayerCommand>(
            new TypedParameter(typeof(TilePyramidDocumentView), this));

    public ICommand MoveUpAnnotationLayerCommand => _moveUpAnnotationLayerCommand ??=
        LifetimeScope.Resolve<MoveUpAnnotationLayerCommand>(
            new TypedParameter(typeof(TilePyramidDocumentView), this));

    public ICommand SetAnnotationLayerLockStateCommand => _setAnnotationLayerLockStateCommand ??=
        LifetimeScope.Resolve<SetAnnotationLayerLockStateCommand>(
            new TypedParameter(typeof(TilePyramidDocumentView), this));

    public ICommand SetAnnotationLayerVisibleStateCommand => _setAnnotationLayerVisibleStateCommand ??=
        LifetimeScope.Resolve<SetAnnotationLayerVisibleStateCommand>(
            new TypedParameter(typeof(TilePyramidDocumentView), this));

    public ObservableCollection<AnnotationLayerItem> AnnotationLayers { get; } = new();

    public ListCollectionView? AnnotationListView { get; private set; }

    public int SavedAnnotationCount => DocumentState.Annotations.Count;

    public AnnotationLayerItem? SelectedAnnotationLayer
    {
        get => _selectedAnnotationLayer;
        set
        {
            if (value is null || IsEditingAnnotation || _refreshingAnnotationLayers
                || ReferenceEquals(value, _selectedAnnotationLayer) || !AnnotationLayers.Contains(value))
            {
                return;
            }

            SelectAnnotationLayer(value);
        }
    }

    public bool CanMoveAnnotationLayerUp => CanMoveAnnotationLayer(SelectedAnnotationLayer, true);

    public bool CanMoveAnnotationLayerDown => CanMoveAnnotationLayer(SelectedAnnotationLayer, false);

    public bool CanDeleteAnnotationLayer => CanDeleteAnnotationLayerItem(SelectedAnnotationLayer);

    private IEnumerable<TilePyramidTabItem> SharedLayerTabs => AgentDocuments.Items.Where(tab =>
        ReferenceEquals(tab.DocumentState.AnnotationLayers, DocumentState.AnnotationLayers));

    public string AnnotationLayerHint => SelectedAnnotationLayer switch
    {
        { IsLocked: true } => "The current layer is locked. Unlock it to create and edit annotations.",
        { IsVisible: false } => "The current layer is hidden. Show it to create annotations and edit geometry.",
        _ => "New annotations will be added to the current layer; upper layers cover lower layers."
    };

    public bool CanModifyAnnotationLayer(AnnotationLayerItem? layer)
    {
        return CanStartAnnotationEdit && layer is not null && AnnotationLayers.Contains(layer)
               && ReferenceEquals(DocumentState.AnnotationLayers.Definitions.Find(layer.Id), layer.Definition);
    }

    public bool CanMoveAnnotationLayer(AnnotationLayerItem? layer, bool up)
    {
        return CanModifyAnnotationLayer(layer) && (up
            ? DocumentState.AnnotationLayers.CanMoveUp(layer!.Id)
            : DocumentState.AnnotationLayers.CanMoveDown(layer!.Id));
    }

    public bool CanDeleteAnnotationLayerItem(AnnotationLayerItem? layer)
    {
        return CanModifyAnnotationLayer(layer) && layer is { Id.Length: > 0, IsLocked: false }
                                               && !DocumentState.Annotations.Any(item => item.LayerId == layer.Id);
    }

    private void InitializeAnnotationLayers()
    {
        // Group by stable IDs; display names can change and need not be unique.
        AnnotationListView = new ListCollectionView(DocumentState.Annotations);
        AnnotationListView.GroupDescriptions.Add(_annotationLayerGrouping);
        // Regroup only the affected annotation when its LayerId changes, including batch property edits.
        AnnotationListView.LiveGroupingProperties.Add(nameof(Annotation.LayerId));
        AnnotationListView.IsLiveGrouping = true;
        RefreshAnnotationLayers();
        OnPropertyChanged(nameof(AnnotationListView));
        Observable.FromEventPattern<AnnotationItemsChangedEventArgs>(
                h => DocumentState.Annotations.Changed += h, h => DocumentState.Annotations.Changed -= h)
            .Subscribe(_ => OnPropertyChanged(nameof(SavedAnnotationCount))).DisposeWith(CompositeDisposable);
        Observable.FromEventPattern(h => AgentDocuments.LayerContentsChanged += h,
                h => AgentDocuments.LayerContentsChanged -= h)
            .Subscribe(_ => NotifyAnnotationLayerCommands()).DisposeWith(CompositeDisposable);
    }

    private void RefreshAnnotationLayers()
    {
        if (_refreshingAnnotationLayers)
        {
            return;
        }

        _refreshingAnnotationLayers = true;
        try
        {
            var settings = DocumentState.AnnotationLayers;
            settings.GetOrCreate("");
            // Recognition results retain target IDs without keeping deleted layers registered.
            foreach (var id in DocumentState.Annotations.Select(item => item.LayerId).Distinct(StringComparer.Ordinal))
            {
                settings.GetOrCreate(id);
            }

            var definitions = settings.Definitions.OrderByDescending(layer => layer.Order)
                .ThenByDescending(layer => layer.Id, StringComparer.Ordinal).ToArray();
            var ids = definitions.Select(layer => layer.Id).ToHashSet(StringComparer.Ordinal);
            for (var i = AnnotationLayers.Count - 1; i >= 0; i--)
            {
                if (!ids.Contains(AnnotationLayers[i].Id))
                {
                    AnnotationLayers.RemoveAt(i);
                }
            }

            for (var i = 0; i < definitions.Length; i++)
            {
                var definition = definitions[i];
                var row = AnnotationLayers.FirstOrDefault(layer => layer.Id == definition.Id);
                if (row is null)
                {
                    row = new AnnotationLayerItem(definition, settings);
                    AnnotationLayers.Insert(i, row);
                }
                else if (AnnotationLayers.IndexOf(row) != i)
                {
                    AnnotationLayers.Move(AnnotationLayers.IndexOf(row), i);
                }

                // Keep the group header's row even when a definition is replaced with the same ID.
                row.Refresh(definition);
            }

            var selected = AnnotationLayers.FirstOrDefault(layer => layer.Id == DocumentState.ActiveAnnotationLayerId)
                           ?? AnnotationLayers.Single(layer => layer.Id == "");
            if (!ReferenceEquals(selected, _selectedAnnotationLayer))
            {
                _editingSession?.Cancel();
                SelectAnnotationLayer(selected);
            }
            else
            {
                NotifyAnnotationLayerSelection();
            }

            RefreshAnnotationGroups(definitions);
        }
        finally
        {
            _refreshingAnnotationLayers = false;
        }
    }

    private void RefreshAnnotationGroups(IReadOnlyList<AnnotationLayerDefinition> layers)
    {
        if (AnnotationListView is not { } view)
        {
            return;
        }

        var layerIds = layers.Select(layer => layer.Id).ToArray();
        if (_annotationLayerGrouping.GroupNames.Cast<string>().SequenceEqual(layerIds, StringComparer.Ordinal))
        {
            // Item bindings and live grouping handle property changes without resetting the list.
            return;
        }

        using (view.DeferRefresh())
        {
            // Explicit group names retain empty layers and define their top-to-bottom order.
            _annotationLayerGrouping.GroupNames.Clear();
            foreach (var id in layerIds)
            {
                _annotationLayerGrouping.GroupNames.Add(id);
            }
        }
    }

    private void SelectAnnotationLayer(AnnotationLayerItem layer)
    {
        _selectedAnnotationLayer = layer;
        DocumentState.ActiveAnnotationLayerId = layer.Id;
        _annotationEditor!.CreationLayerId = layer.Id;

        NotifyAnnotationLayerSelection();
        NotifyAnnotationAvailability();


        SetCurrentAnnotationLayerCommand.RaiseCanExecuteChanged();
    }

    private void NotifyAnnotationLayerSelection()
    {
        OnPropertyChanged(nameof(SelectedAnnotationLayer));
        OnPropertyChanged(nameof(CanMoveAnnotationLayerUp));
        OnPropertyChanged(nameof(CanMoveAnnotationLayerDown));
        OnPropertyChanged(nameof(CanDeleteAnnotationLayer));
        OnPropertyChanged(nameof(AnnotationLayerHint));
    }

    private void NotifyAnnotationLayerCommands()
    {
        OnPropertyChanged(nameof(CanMoveAnnotationLayerUp));
        OnPropertyChanged(nameof(CanMoveAnnotationLayerDown));
        OnPropertyChanged(nameof(CanDeleteAnnotationLayer));
        (_newAnnotationLayerDialogCommand as NewAnnotationLayerDialogCommand)?.RaiseCanExecuteChanged();
        (_setAnnotationLayerVisibleStateCommand as SetAnnotationLayerVisibleStateCommand)?.RaiseCanExecuteChanged();
        (_setAnnotationLayerLockStateCommand as SetAnnotationLayerLockStateCommand)?.RaiseCanExecuteChanged();
        (_moveUpAnnotationLayerCommand as MoveUpAnnotationLayerCommand)?.RaiseCanExecuteChanged();
        (_moveDownAnnotationLayerCommand as MoveDownAnnotationLayerCommand)?.RaiseCanExecuteChanged();
        (_editAnnotationLayerCommand as EditAnnotationLayerCommand)?.RaiseCanExecuteChanged();
        (_deleteAnnotationLayerCommand as DeleteAnnotationLayerCommand)?.RaiseCanExecuteChanged();
    }

    public void AddAnnotationLayer(string name)
    {
        if (!CanStartAnnotationEdit)
        {
            throw new InvalidOperationException(
                L.T("Cannot create a layer in the current document. Finish editing annotations and try again."));
        }

        var definition = TilePyramidLayerOperations.Create(DocumentState.AnnotationLayers, name);
        SelectedAnnotationLayer = AnnotationLayers.Single(layer => layer.Id == definition.Id);
    }


    public void MoveAnnotationLayer(bool up)
    {
        if (SelectedAnnotationLayer is { } layer)
        {
            MoveAnnotationLayer(layer, up);
        }
    }

    public void MoveAnnotationLayer(AnnotationLayerItem layer, bool up)
    {
        if (!CanMoveAnnotationLayer(layer, up))
        {
            return;
        }

        if (up)
        {
            DocumentState.AnnotationLayers.MoveUp(layer.Id);
        }
        else
        {
            DocumentState.AnnotationLayers.MoveDown(layer.Id);
        }
    }

    public void DeleteAnnotationLayer(AnnotationLayerItem layer)
    {
        if (!CanDeleteAnnotationLayerItem(layer))
        {
            return;
        }

        TilePyramidLayerOperations.Delete(DocumentState, layer.Id,
            SharedLayerTabs.Select(tab => tab.DocumentState));
    }

    public void SetAnnotationLayerVisibleState(AnnotationLayerItem layer, bool visible)
    {
        if (CanModifyAnnotationLayer(layer))
        {
            DocumentState.AnnotationLayers.SetVisible(layer.Id, visible);
        }
    }

    public void SetAnnotationLayerLockState(AnnotationLayerItem layer, bool locked)
    {
        if (CanModifyAnnotationLayer(layer))
        {
            DocumentState.AnnotationLayers.SetLocked(layer.Id, locked);
        }
    }

    public void RenameAnnotationLayer(AnnotationLayerItem layer, string name)
    {
        if (!CanModifyAnnotationLayer(layer))
        {
            throw new InvalidOperationException(
                L.T("The layer has been removed or cannot be edited in the current document."));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(L.T("The layer name cannot be empty."), nameof(name));
        }

        DocumentState.AnnotationLayers.Rename(layer.Id, name);
    }
}
