using System.Collections.ObjectModel;
using PropertyChanged;
using ZYC.TilePyramid.Annotations;
using ZYC.TilePyramid.Core;
using ZYC.TilePyramid.Core.Viewports;

namespace ZYC.Framework.Modules.TilePyramid.Abstractions;

/// <summary>Stores view preferences, the last viewport and annotations for a pyramid document.</summary>
[AddINotifyPropertyChangedInterface]
public class TilePyramidDocumentState
{
    /// <summary>The minimap's standard outer width in DIP.</summary>
    public const double DefaultMinimapWidth = 240;

    /// <summary>The minimap's standard outer height in DIP.</summary>
    public const double DefaultMinimapHeight = 190;

    /// <summary>Gets or sets floating panel layouts keyed by stable panel identifiers.</summary>
    public FloatingPanelStateCollection FloatingPanels { get; set; } = new()
    {
        [FloatingPanelIds.Minimap] = new FloatingPanelState
        {
            Position = FloatPosition.TopRight,
            Width = DefaultMinimapWidth,
            Height = DefaultMinimapHeight
        },
        [FloatingPanelIds.DocumentInfo] = new FloatingPanelState
        {
            Position = FloatPosition.BottomLeft,
            Width = 300
        },
        [FloatingPanelIds.Annotations] = new FloatingPanelState
        {
            Position = FloatPosition.TopLeft,
            Width = 400,
            FillHeight = true
        },
        [FloatingPanelIds.Recognition] = new FloatingPanelState
        {
            Position = FloatPosition.BottomRight,
            Width = 420,
            FillHeight = true,
            IsVisible = false
        },
        [FloatingPanelIds.Debug] = new FloatingPanelState
        {
            Position = FloatPosition.Top,
            Width = 420,
            IsVisible = false
        }
    };
    //TODO-zyc Support injected config from outside

    /// <summary>Gets or sets the label image rotation in degrees.</summary>
    public int LabelImageRotation { get; set; }

    /// <summary>Gets or sets the viewport, or null to initially fit the image to the window.</summary>
    public TilePyramidNavigationState? Navigation { get; set; }

    /// <summary>Last selected focal plane, or null to initially use the source's default plane.</summary>
    public int? FocalPlaneIndex { get; set; }

    /// <summary>Gets or sets the committed annotations in full-resolution image coordinates.</summary>
    public AnnotationCollection<Annotation> Annotations { get; set; } = new();

    /// <summary>Image-owned recognition batches and pending candidates, shared by tabs and persisted with annotations.</summary>
    public RecognitionBatchCollection RecognitionBatches { get; set; } = new();

    /// <summary>Shared layer names, visibility, locks and stacking order.</summary>
    public AnnotationLayerSettings AnnotationLayers { get; set; } = new();

    /// <summary>The layer selected for new annotations in this tab.</summary>
    public string ActiveAnnotationLayerId { get; set; } = "";

    /// <summary>Get or sets is agent tasks panel opened.</summary>
    public bool IsAgentTasksPanelOpened { get; set; }

    /// <summary>Copies the mutable collection and items; shapes and provenance are immutable.</summary>
    public AnnotationCollection<Annotation> CloneAnnotations()
    {
        var annotations = new AnnotationCollection<Annotation>();
        foreach (var item in Annotations)
        {
            var copy = new Annotation { Id = item.Id };
            copy.UpdateFrom(item);
            annotations.Add(copy);
        }

        return annotations;
    }

    /// <summary>Copies view state for a tab while sharing live annotations, recognition batches and layer settings.</summary>
    public TilePyramidDocumentState CreateTabState()
    {
        return new TilePyramidDocumentState
        {
            FloatingPanels = FloatingPanels.Clone(),
            LabelImageRotation = LabelImageRotation,
            Navigation = Navigation,
            FocalPlaneIndex = FocalPlaneIndex,
            Annotations = Annotations,
            RecognitionBatches = RecognitionBatches,
            AnnotationLayers = AnnotationLayers,
            ActiveAnnotationLayerId = ActiveAnnotationLayerId
        };
    }

    /// <summary>Moves a panel to the end of the destination anchor group.</summary>
    /// <param name="panelId">The stable identifier of a panel in <see cref="FloatingPanels" />.</param>
    /// <param name="position">The destination anchor.</param>
    public void MoveFloatingPanel(string panelId, FloatPosition position)
    {
        var panel = FloatingPanels[panelId];
        if (panel.Position == position)
        {
            return;
        }

        var peers = FloatingPanels.Values
            .Where(other => !ReferenceEquals(other, panel) && other.Position == position)
            .OrderBy(other => other.Order).ToArray();
        for (var index = 0; index < peers.Length; index++)
        {
            peers[index].Order = index;
        }

        panel.Order = peers.Length;
        panel.Position = position;
    }

    /// <summary>Creates an independent snapshot for persistent storage.</summary>
    public TilePyramidDocumentState Clone()
    {
        var snapshot = CreateTabState();
        snapshot.Annotations = CloneAnnotations();
        snapshot.RecognitionBatches = new RecognitionBatchCollection();
        foreach (var batch in RecognitionBatches)
        {
            snapshot.RecognitionBatches.Add(batch.Clone());
        }

        snapshot.AnnotationLayers = new AnnotationLayerSettings
        {
            HiddenLayerIds = new ObservableCollection<string>(AnnotationLayers.HiddenLayerIds),
            Definitions = new AnnotationLayerCollection()
        };
        foreach (var layer in AnnotationLayers.Definitions)
        {
            snapshot.AnnotationLayers.Definitions.Add(new AnnotationLayerDefinition
            {
                Id = layer.Id,
                Name = layer.Name,
                IsLocked = layer.IsLocked,
                Order = layer.Order
            });
        }

        return snapshot;
    }
}