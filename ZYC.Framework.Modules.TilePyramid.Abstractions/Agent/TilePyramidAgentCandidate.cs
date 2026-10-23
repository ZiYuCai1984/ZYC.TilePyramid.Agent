namespace ZYC.Framework.Modules.TilePyramid.Abstractions.Agent;

/// <summary>A candidate snapshot or submission. Score is a finite detector-supplied value, not a calibrated probability.</summary>
public record TilePyramidAgentCandidate
{
    /// <summary>Stable, non-empty feature ID. Reuse it for incremental updates within one batch.</summary>
    public Guid Id { get; init; }
    /// <summary>Human-readable display label.</summary>
    public string Label { get; init; } = "";
    /// <summary>Icon text accepted by the annotation UI, such as Triangle, Rhombus or an emoji.</summary>
    public string Icon { get; init; } = "Triangle";
    /// <summary>Free-form notes, preserved when the candidate is reviewed into a saved annotation.</summary>
    public string Comments { get; init; } = "";
    /// <summary>Finite detector-supplied score.</summary>
    public double Score { get; init; }
    /// <summary>Geometry in image pixels, or in crop pixels when SubmitCandidatesAsync receives a cropId.</summary>
    public TilePyramidAgentShape Shape { get; init; } = new();
    /// <summary>
    ///     Existing layer ID or a target ID retained by this image's recognition results; empty selects the default layer.
    ///     Existing source and target layers must be unlocked. A missing target is recreated only when an annotation is created.
    /// </summary>
    public string LayerId { get; init; } = "";
    /// <summary>ARGB outline color, defaulting to the recognition candidate color.</summary>
    public uint StrokeArgb { get; init; } = 0xFFFFBE55;
}
