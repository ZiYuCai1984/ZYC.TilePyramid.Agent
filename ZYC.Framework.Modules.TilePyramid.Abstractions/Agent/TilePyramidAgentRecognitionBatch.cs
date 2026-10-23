namespace ZYC.Framework.Modules.TilePyramid.Abstractions.Agent;

/// <summary>A detached batch summary. Ending submissions does not imply that all candidates have been reviewed.</summary>
public record TilePyramidAgentRecognitionBatch(
    Guid Id,
    string ImageId,
    string Model,
    int FocalPlaneIndex,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt,
    int CandidateCount,
    int AcceptedCount,
    int RejectedCount)
{
    /// <summary>The associated task ID in this application session, or null for a batch loaded from storage.</summary>
    public Guid? TaskId { get; init; }

    /// <summary>Whether this batch still accepts candidate submissions.</summary>
    public bool IsOpen => EndedAt is null;
}
