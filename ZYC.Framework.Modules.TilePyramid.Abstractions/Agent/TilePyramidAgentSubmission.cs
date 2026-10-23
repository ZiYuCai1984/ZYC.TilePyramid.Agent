namespace ZYC.Framework.Modules.TilePyramid.Abstractions.Agent;

/// <summary>Counts for one atomic submission. Applied includes both new and updated candidates.</summary>
public record TilePyramidAgentSubmission(Guid BatchId, int Applied, int SkippedReviewed);