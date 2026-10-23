using ZYC.Framework.Modules.TilePyramid.Abstractions;
using ZYC.TilePyramid.Annotations;

namespace ZYC.Framework.Modules.TilePyramid;

/// <summary>Shared layer mutations for the UI and agent service, called on the owning UI thread.</summary>
internal static class TilePyramidLayerOperations
{
    public static AnnotationLayerDefinition Create(AnnotationLayerSettings settings, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        settings.GetOrCreate("");
        var layer = new AnnotationLayerDefinition
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Order = settings.Definitions.Max(item => item.Order)
        };
        settings.Definitions.Add(layer);
        // Reorder instead of incrementing an order that may already be int.MaxValue.
        while (settings.MoveUp(layer.Id))
        {
        }

        return layer;
    }

    public static void Delete(TilePyramidDocumentState state, string layerId,
        IEnumerable<TilePyramidDocumentState> sharedStates)
    {
        if (layerId.Length == 0)
        {
            throw new ArgumentException("The default annotation layer cannot be deleted.", nameof(layerId));
        }

        var settings = state.AnnotationLayers;
        if (settings.IsLocked(layerId))
        {
            throw new InvalidOperationException($"Annotation layer '{layerId}' is locked.");
        }

        if (state.Annotations.Any(item => item.LayerId == layerId))
        {
            throw new InvalidOperationException("Only layers without annotations can be deleted.");
        }

        // Include tabs whose views have not been created.
        foreach (var other in sharedStates)
        {
            if (other.ActiveAnnotationLayerId == layerId)
            {
                other.ActiveAnnotationLayerId = "";
            }
        }

        settings.SetVisible(layerId, true);
        if (settings.Definitions.Find(layerId) is { } layer)
        {
            settings.Definitions.Remove(layer);
        }
    }
}
