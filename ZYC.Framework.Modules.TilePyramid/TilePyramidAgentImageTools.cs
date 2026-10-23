using System.Text.Json;
using ModelContextProtocol.Protocol;
using ZYC.CoreToolkit.Extensions.Autofac.Attributes;
using ZYC.Framework.Modules.TilePyramid.Abstractions.Agent;
using ZYC.TilePyramid.Core.Viewports;

namespace ZYC.Framework.Modules.TilePyramid;

[RegisterSingleInstanceAs(typeof(ITilePyramidAgentImageTools))]
internal class TilePyramidAgentImageTools : ITilePyramidAgentImageTools
{
    private readonly ITilePyramidAgentService _service;

    public TilePyramidAgentImageTools(ITilePyramidAgentService service)
    {
        _service = service;
    }

    public async Task<CallToolResult> ReadRegionAsync(
        Guid documentId,
        ImageRect region,
        int maximumWidth = 1024,
        int maximumHeight = 1024,
        int? focalPlaneIndex = null,
        Guid? taskId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.ReadRegionAsync(
            documentId,
            region,
            maximumWidth,
            maximumHeight,
            focalPlaneIndex,
            taskId,
            cancellationToken).ConfigureAwait(false);
        return new CallToolResult
        {
            Content =
            [
                new TextContentBlock { Text = JsonSerializer.Serialize(result.Crop) },
                ImageContentBlock.FromBytes(result.Data, result.MimeType)
            ],
            StructuredContent = JsonSerializer.SerializeToElement(result.Crop)
        };
    }
}
