using ZYC.Framework.Core.Localizations;
using ZYC.Framework.Modules.TilePyramid.Abstractions.Agent;
using ZYC.TilePyramid.Annotations;

namespace ZYC.Framework.Modules.TilePyramid;

internal partial class TilePyramidAgentService
{
    public Task<TilePyramidAgentLayer[]> GetLayersAsync(Guid documentId,
        CancellationToken cancellationToken = default)
    {
        return OnUIAsync(() => GetLayers(GetTab(documentId)), cancellationToken);
    }

    public Task<TilePyramidAgentLayer> CreateLayerAsync(Guid documentId, string name,
        CancellationToken cancellationToken = default)
    {
        var layerName = RequireLayerName(name);
        return OnUIAsync(() =>
        {
            var tab = GetTab(documentId);
            EnsureDocumentWriteAllowed(tab);
            RegisterLayers(tab);
            var layer = TilePyramidLayerOperations.Create(tab.DocumentState.AnnotationLayers, layerName);

            return GetLayers(tab).Single(item => item.Id == layer.Id);
        }, cancellationToken);
    }

    public Task<TilePyramidAgentLayer> UpdateLayerAsync(Guid documentId, string layerId, string? name = null,
        bool? isVisible = null, bool? isLocked = null, CancellationToken cancellationToken = default)
    {
        var layerName = name is null ? null : RequireLayerName(name);
        return OnUIAsync(() =>
        {
            var tab = GetTab(documentId);
            RequireLayer(tab, layerId);
            EnsureDocumentWriteAllowed(tab);
            var settings = tab.DocumentState.AnnotationLayers;
            if (layerName is not null || isVisible.HasValue || isLocked.HasValue)
            {
                // Keep a layer registered when its only previous reference was a hidden-layer entry.
                settings.GetOrCreate(layerId);
            }

            if (layerName is not null)
            {
                settings.Rename(layerId, layerName);
            }

            if (isVisible is { } visible)
            {
                settings.SetVisible(layerId, visible);
            }

            if (isLocked is { } locked)
            {
                settings.SetLocked(layerId, locked);
            }

            return GetLayers(tab).Single(item => item.Id == layerId);
        }, cancellationToken);
    }

    public Task<TilePyramidAgentLayer[]> MoveLayerAsync(Guid documentId, string layerId, bool up,
        CancellationToken cancellationToken = default)
    {
        return OnUIAsync(() =>
        {
            var tab = GetTab(documentId);
            RequireLayer(tab, layerId);
            EnsureDocumentWriteAllowed(tab);
            RegisterLayers(tab);
            if (up)
            {
                tab.DocumentState.AnnotationLayers.MoveUp(layerId);
            }
            else
            {
                tab.DocumentState.AnnotationLayers.MoveDown(layerId);
            }

            return GetLayers(tab);
        }, cancellationToken);
    }

    public Task DeleteLayerAsync(Guid documentId, string layerId, CancellationToken cancellationToken = default)
    {
        return OnUIAsync(() =>
        {
            var tab = GetTab(documentId);
            RequireLayer(tab, layerId);
            EnsureDocumentWriteAllowed(tab);
            TilePyramidLayerOperations.Delete(tab.DocumentState, layerId,
                GetSharedLayerTabs(tab).Select(other => other.DocumentState));

            return true;
        }, cancellationToken);
    }

    public Task<Annotation[]> MoveAnnotationsToLayerAsync(
        Guid documentId,
        Guid[] annotationIds,
        string layerId,
        CancellationToken cancellationToken = default)
    {
        // Do not retain the caller's mutable array across the dispatcher boundary.
        var ids = annotationIds.ToArray();
        if (ids.Length == 0 || ids.Contains(Guid.Empty) || ids.Distinct().Count() != ids.Length)
        {
            throw new ArgumentException("Provide one or more unique, non-empty annotation IDs.", nameof(annotationIds));
        }

        return OnUIAsync(() =>
        {
            var tab = GetTab(documentId);
            EnsureDocumentWriteAllowed(tab);
            RequireUnlockedLayer(tab, layerId);
            var annotations = tab.AnnotationImage.Annotations;
            var items = ids.Select(id => annotations.Find(id)
                                         ?? throw new KeyNotFoundException($"Annotation '{id}' was not found."))
                .ToArray();
            foreach (var sourceLayerId in items.Select(item => item.LayerId).Distinct())
            {
                RequireUnlockedLayer(tab, sourceLayerId);
            }

            // All IDs and both sides of every move are valid before any state changes.
            tab.DocumentState.AnnotationLayers.GetOrCreate(layerId);
            using (annotations.BeginUpdate())
            {
                foreach (var item in items)
                {
                    item.LayerId = layerId;
                }
            }

            return items.Select(CloneAnnotation).ToArray();
        }, cancellationToken);
    }

    private IEnumerable<TilePyramidTabItem> GetSharedLayerTabs(TilePyramidTabItem tab)
    {
        return _documents.Items.Where(other =>
            ReferenceEquals(other.DocumentState.AnnotationLayers, tab.DocumentState.AnnotationLayers));
    }

    private void EnsureDocumentWriteAllowed(TilePyramidTabItem tab)
    {
        foreach (var other in GetSharedLayerTabs(tab))
        {
            if (other.CreatedView?.IsEditingAnnotation == true)
            {
                throw new InvalidOperationException(
                    "An annotation is being manually edited in a tab sharing this document's layers.");
            }
        }
    }

    private void RequireLayer(TilePyramidTabItem tab, string layerId, bool allowRecognitionTarget = false)
    {
        var settings = tab.DocumentState.AnnotationLayers;
        // Only recognition writes may reuse deleted targets without recreating their layers.
        if (layerId.Length == 0 || settings.Definitions.Find(layerId) is not null
                                || settings.HiddenLayerIds.Contains(layerId)
                                || tab.AnnotationImage.Annotations.Any(item => item.LayerId == layerId)
                                || (allowRecognitionTarget && tab.AnnotationImage.RecognitionLayerIds.Contains(layerId)))
        {
            return;
        }

        throw new KeyNotFoundException($"Annotation layer '{layerId}' was not found. Create a layer first.");
    }

    private void RequireUnlockedLayer(TilePyramidTabItem tab, string layerId, bool allowRecognitionTarget = false)
    {
        RequireLayer(tab, layerId, allowRecognitionTarget);
        // This check is independent of the view's existence and lifecycle.
        if (tab.DocumentState.AnnotationLayers.IsLocked(layerId))
        {
            throw new InvalidOperationException($"Annotation layer '{layerId}' is locked.");
        }
    }

    private TilePyramidAgentLayer[] GetLayers(TilePyramidTabItem tab)
    {
        var settings = tab.DocumentState.AnnotationLayers;
        var annotations = tab.AnnotationImage.Annotations.GroupBy(item => item.LayerId)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        var candidates = tab.AnnotationImage.Candidates.GroupBy(item => item.LayerId)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        var ids = settings.Definitions.Select(layer => layer.Id).Concat(settings.HiddenLayerIds)
            .Concat(annotations.Keys).Append("").Distinct(StringComparer.Ordinal);
        return ids.OrderByDescending(settings.GetOrder).ThenByDescending(id => id, StringComparer.Ordinal)
            .Select(id => new TilePyramidAgentLayer(id,
                string.IsNullOrEmpty(settings.GetName(id)) ? L.T("Default") : settings.GetName(id),
                settings.IsVisible(id), settings.IsLocked(id), settings.GetOrder(id),
                annotations.GetValueOrDefault(id), candidates.GetValueOrDefault(id)))
            .ToArray();
    }

    private void RegisterLayers(TilePyramidTabItem tab)
    {
        foreach (var layer in GetLayers(tab))
        {
            tab.DocumentState.AnnotationLayers.GetOrCreate(layer.Id);
        }
    }

    private static string RequireLayerName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A non-blank layer name is required.", nameof(name));
        }

        return name.Trim();
    }
}
