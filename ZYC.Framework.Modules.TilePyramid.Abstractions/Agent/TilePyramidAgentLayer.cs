namespace ZYC.Framework.Modules.TilePyramid.Abstractions.Agent;

/// <summary>A detached snapshot of a document's annotation layer.</summary>
/// <param name="Id">Stable, case-sensitive ID; the empty string denotes the default layer.</param>
/// <param name="Name">Display name, falling back to the ID or the default-layer label when unnamed.</param>
/// <param name="IsVisible">Whether this layer is displayed; each annotation also has its own visibility.</param>
/// <param name="IsLocked">Whether modifying annotations and candidates in this layer is forbidden.</param>
/// <param name="Order">Higher values draw above lower values; ties are ordered by ordinal ID.</param>
/// <param name="AnnotationCount">Saved annotations in the shared collection, including hidden annotations.</param>
/// <param name="CandidateCount">Candidates across all loaded tabs sharing these layer settings.</param>
public record TilePyramidAgentLayer(
    string Id,
    string Name,
    bool IsVisible,
    bool IsLocked,
    int Order,
    int AnnotationCount,
    int CandidateCount);