using ModelContextProtocol.Protocol;
using ZYC.Framework.Abstractions.MCP;
using ZYC.Framework.Modules.TilePyramid.Abstractions;
using ZYC.TilePyramid.Core.Viewports;

namespace ZYC.Framework.Modules.TilePyramid;

//!WARNING Keep this internal MCP adapter interface in the implementation project.
// Its CallToolResult return type comes from ModelContextProtocol.Core; moving it to Abstractions
// would make that project depend on the MCP SDK. ITilePyramidAgentService already provides the
// SDK-independent business contract; this adapter only wraps its image result in MCP content blocks.

/// <summary>MCP-specific image transport. The public business service remains independent of the MCP SDK.</summary>
[ExposeToMCP(Name = TilePyramidModuleConstants.MCPToolName)]
internal interface ITilePyramidAgentImageTools
{
    /// <summary>
    ///     Reads an exact unrotated rectangle from a loaded document in full-resolution image pixels. Returns PNG image
    ///     content and crop metadata. Maximum output width/height are 1..1024; focalPlaneIndex is zero-based and defaults to
    ///     the current plane. Use the returned CropId to submit candidates in the returned image's pixel coordinates. Crop IDs
    ///     expire when the document closes. Supply taskId for recognition reads to pin scope and focal plane and to cancel
    ///     pending reads when the task stops. Without taskId this is independent, read-only inspection.
    /// </summary>
    Task<CallToolResult> ReadRegionAsync(Guid documentId, ImageRect region,
        int maximumWidth = 1024, int maximumHeight = 1024, int? focalPlaneIndex = null,
        Guid? taskId = null, CancellationToken cancellationToken = default);
}
