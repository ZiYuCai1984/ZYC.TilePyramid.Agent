using ZYC.Framework.Abstractions.MCP;
using ZYC.TilePyramid.Annotations;
using ZYC.TilePyramid.Core.Viewports;

namespace ZYC.Framework.Modules.TilePyramid.Abstractions.Agent;

/// <summary>Agent access to loaded TilePyramid tabs. All methods may be called from any thread.</summary>
[ExposeToMCP(Name = TilePyramidModuleConstants.MCPToolName)]
public partial interface ITilePyramidAgentService
{
    /// <summary>Lists loaded documents. DocumentId identifies a tab, even when several tabs show the same image.</summary>
    Task<TilePyramidAgentDocumentInfo[]> ListDocumentsAsync(CancellationToken cancellationToken = default);

    /// <summary>Gets a document's source geometry, metadata and all image-owned recognition batch summaries.</summary>
    Task<TilePyramidAgentDocumentInfo> GetDocumentInfoAsync(Guid documentId,
        CancellationToken cancellationToken = default);

    /// <summary>Gets the actual viewport. Visible geometry is unavailable until the view has been laid out.</summary>
    Task<TilePyramidAgentViewport> GetViewportAsync(Guid documentId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Sets center in full-resolution pixels, scale in view units per image pixel, rotation in degrees and an
    ///     optional zero-based focal plane, which must match the task's fixed plane. Requires a running task belonging to
    ///     this tab with user-granted viewport control. Returns the actual constrained viewport. Fails during manual editing.
    /// </summary>
    Task<TilePyramidAgentViewport> SetViewportAsync(Guid documentId, Guid taskId, TilePyramidNavigationState navigation,
        int? focalPlaneIndex = null, CancellationToken cancellationToken = default);


    /// <summary>Gets snapshots of formal annotations. Recognition results, including rejected results, are queried separately.</summary>
    Task<Annotation[]> GetAnnotationsAsync(Guid documentId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Updates a saved annotation's label, ARGB color, icon, notes or layer and returns a detached snapshot.
    ///     Null fields remain unchanged; empty label, icon or comments clear that field, and an empty layer ID selects
    ///     the default layer. Label and icon are trimmed; notes preserve whitespace. Both source and target layers must
    ///     exist and be unlocked; hidden layers are allowed. Validates all fields before writing. Preserves geometry,
    ///     visibility and recognition source reference without changing the original result. Fails during manual editing in
    ///     any tab
    ///     sharing these layers.
    /// </summary>
    Task<Annotation> UpdateAnnotationAsync(Guid documentId, Guid annotationId, string? label = null,
        uint? strokeArgb = null, string? icon = null, string? comments = null, string? layerId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Lists detached layer snapshots from top to bottom. Includes the default layer (empty ID) and layers referenced
    ///     by formal annotations or visibility settings. Deleted layers referenced only by recognition results are omitted.
    ///     Pending candidate counts cover all batches without counting shared tabs twice. Does not create a view or modify state.
    /// </summary>
    Task<TilePyramidAgentLayer[]> GetLayersAsync(Guid documentId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Creates a visible, unlocked topmost layer with a non-blank name and a generated stable ID. Does not change the
    ///     UI's selected layer. Fails while any tab sharing these layers is manually editing annotations.
    /// </summary>
    Task<TilePyramidAgentLayer> CreateLayerAsync(Guid documentId, string name,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Updates a layer's name, visibility or lock; null fields remain unchanged and blank names are rejected. Layer
    ///     IDs are case-sensitive and must exist. Layer settings may be changed while locked, including unlocking. Fails
    ///     during manual editing in any tab sharing these layers.
    /// </summary>
    Task<TilePyramidAgentLayer> UpdateLayerAsync(Guid documentId, string layerId, string? name = null,
        bool? isVisible = null, bool? isLocked = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Moves a layer one position up (above its neighbor) when up is true, or down otherwise. At a boundary this is a
    ///     no-op. Locked layers may be reordered. Returns the resulting top-to-bottom layer snapshots. Fails during manual
    ///     editing in any tab sharing these layers.
    /// </summary>
    Task<TilePyramidAgentLayer[]> MoveLayerAsync(Guid documentId, string layerId, bool up,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Deletes an unlocked, non-default layer when it has no formal annotations. Recognition results retain their target
    ///     layer IDs; missing layers are recreated only when a recognition result creates an annotation. Resets affected UI
    ///     selections to the default layer. Unknown IDs are rejected. Fails during manual editing in any tab sharing these layers.
    /// </summary>
    Task DeleteLayerAsync(Guid documentId, string layerId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Moves saved annotations to an existing layer and returns detached snapshots in request order. Requires one or
    ///     more unique non-empty annotation IDs and unlocked source and target layers; hidden layers are allowed. Validates
    ///     the entire request before changing any annotation. Preserves geometry, visibility and recognition provenance. Fails
    ///     during manual editing in any tab sharing these layers.
    /// </summary>
    Task<Annotation[]> MoveAnnotationsToLayerAsync(Guid documentId, Guid[] annotationIds, string layerId,
        CancellationToken cancellationToken = default);

    /// <summary>Lists detached summaries of all recognition batches shared by tabs of this image, in creation order.</summary>
    Task<TilePyramidAgentRecognitionBatch[]> GetRecognitionBatchesAsync(Guid documentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets the specified batch's unreviewed candidates in full-resolution image coordinates. Candidate IDs are
    ///     unique within a batch.
    /// </summary>
    Task<TilePyramidAgentCandidate[]>
        GetCandidatesAsync(Guid documentId, Guid batchId, CancellationToken cancellationToken = default);

    /// <summary>Gets all persistent recognition results in a batch, including review decisions and annotation associations.</summary>
    Task<RecognitionFeature[]> GetRecognitionFeaturesAsync(Guid documentId, Guid batchId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Starts a background task and an image-owned recognition batch. Returns the batch with its session TaskId.
    ///     Region defaults to the whole image; focalPlaneIndex defaults to the current plane; layerId defaults to the default
    ///     layer. These values are fixed for the task. The target layer must exist and be unlocked. Preserves existing results.
    ///     Captures the application's AutoAcceptRecognitionResults default in the task; later configuration changes do not
    ///     affect it. Query the task snapshot to inspect the captured policy.
    ///     ReportTaskProgressAsync must renew the 120-second lease at least every 30 seconds, including during model calls.
    ///     Fails while any tab sharing these layers is manually editing annotations. Does not call a model or take viewport control.
    /// </summary>
    Task<TilePyramidAgentRecognitionBatch> BeginRecognitionAsync(Guid documentId, string model,
        string? title = null, ImageRect? region = null, int? focalPlaneIndex = null, string layerId = "",
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Upserts candidates with stable non-empty IDs into the specified open batch belonging to a running session task
    ///     owned by this exact tab. Candidates must lie in the task's fixed region and use its target layer. IDs are scoped to the batch.
    ///     Coordinates are full-resolution
    ///     pixels unless cropId is supplied, in which case they are pixels of that returned crop. Validates the whole request
    ///     before applying it. Reviewed IDs are skipped. Target layers must exist or have an ID retained by this image's
    ///     recognition results. Existing source and target layers must be unlocked; hidden layers are allowed.
    ///     Submission alone does not recreate deleted layers. When the task's captured AutoAcceptRecognitionResults policy
    ///     is enabled, submitted results are immediately accepted, recreating missing target layers as annotations are created.
    ///     Fails during manual editing in any tab sharing these layers.
    /// </summary>
    Task<TilePyramidAgentSubmission> SubmitCandidatesAsync(Guid documentId, Guid batchId,
        TilePyramidAgentCandidate[] candidates, Guid? cropId = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Reports successful completion of the running task owning this batch, releases viewport control and closes
    ///     submissions. All results remain available for review. A stopping or interrupted task cannot complete successfully.
    /// </summary>
    Task EndRecognitionAsync(Guid documentId, Guid batchId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Accepts a Pending result and creates an editable annotation from its original data, on the result's layer.
    ///     Recreates a missing target layer with its original ID and that ID as its display name.
    ///     Repeating acceptance on an Accepted result is a no-op, even if its annotation was deleted. Rejected results must
    ///     first return to Pending. Returns a detached result. Requires unlocked result and associated annotation layers
    ///     and no manual annotation editing in tabs sharing the image; hidden layers are allowed.
    /// </summary>
    Task<RecognitionFeature> AcceptRecognitionAsync(Guid documentId, Guid batchId, Guid featureId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Rejects a Pending result without creating an annotation. Repeating the operation is a no-op.
    ///     Accepted results must first return to Pending. Returns a detached result. Requires an unlocked result layer
    ///     and no manual annotation editing in tabs sharing the image; hidden layers are allowed.
    ///     A missing result layer is allowed and is not recreated.
    /// </summary>
    Task<RecognitionFeature> RejectRecognitionAsync(Guid documentId, Guid batchId, Guid featureId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Returns an Accepted or Rejected result to Pending, deleting its associated annotation and all user edits.
    ///     Clears the association without archiving it and preserves the original recognition data. Repeating the operation
    ///     is a no-op. Returns a detached result. Requires unlocked result and associated annotation layers and no manual
    ///     annotation editing in tabs sharing the image; hidden layers are allowed.
    ///     A missing result layer is allowed and is not recreated.
    /// </summary>
    Task<RecognitionFeature> ResetRecognitionReviewAsync(Guid documentId, Guid batchId, Guid featureId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Creates an editable annotation from an Accepted result's original data, on the result's layer, when no
    ///     annotation is associated. Existing annotations and their edits are preserved. Does not change the decision.
    ///     Recreates a missing target layer only when creating the annotation, using the original ID as its name.
    ///     Returns a detached result. Requires unlocked result and associated annotation layers and no manual annotation
    ///     editing in tabs sharing the image; hidden layers are allowed.
    /// </summary>
    Task<RecognitionFeature> CreateRecognitionAnnotationAsync(Guid documentId, Guid batchId, Guid featureId,
        CancellationToken cancellationToken = default);
}

public partial interface ITilePyramidAgentService
{
    /// <summary>
    ///     Reads an exact, unrotated image region without annotation overlays or padding. Region must lie inside the
    ///     image. Maximum dimensions are 1..1024. Returns PNG bytes and a crop ID usable when submitting crop-local
    ///     coordinates. Supply taskId for recognition work: reads then use the task's fixed scope and focal plane, and are
    ///     cancelled when the task stops or loses its lease. Without taskId this is an independent, read-only inspection.
    /// </summary>
    [MCPIgnore]
    Task<TilePyramidAgentRegion> ReadRegionAsync(Guid documentId, ImageRect region,
        int maximumWidth = 1024, int maximumHeight = 1024, int? focalPlaneIndex = null,
        Guid? taskId = null, CancellationToken cancellationToken = default);
}
