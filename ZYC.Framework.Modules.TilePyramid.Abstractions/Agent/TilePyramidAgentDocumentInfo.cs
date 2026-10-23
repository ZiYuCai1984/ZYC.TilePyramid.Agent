using ZYC.TilePyramid.Annotations;
using ZYC.TilePyramid.Core.Imaging;

namespace ZYC.Framework.Modules.TilePyramid.Abstractions.Agent;

/// <summary>A detached snapshot of a loaded tab. ImageId is shared by tabs of the same source; DocumentId is not.</summary>
public record TilePyramidAgentDocumentInfo(
    Guid DocumentId,
    string ImageId,
    string Title,
    string Uri,
    TilePyramidInfo Info,
    TilePyramidMetadata Metadata,
    int FocalPlaneIndex,
    TilePyramidAgentRecognitionBatch[] RecognitionBatches);
