namespace ZYC.Framework.Modules.TilePyramid.Abstractions.Agent;

/// <summary>
///     An encoded image plus the metadata needed to map its coordinates back to the source. The MCP adapter returns
///     it as image content.
/// </summary>
public record TilePyramidAgentRegion(TilePyramidAgentCrop Crop, string MimeType, byte[] Data);