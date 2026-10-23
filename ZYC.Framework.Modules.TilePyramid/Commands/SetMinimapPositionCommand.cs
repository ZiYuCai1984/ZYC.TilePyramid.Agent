using ZYC.CoreToolkit.Extensions.Autofac.Attributes;
using ZYC.Framework.Core.Commands;
using ZYC.Framework.Modules.TilePyramid.Abstractions;
using ZYC.TilePyramid.Core;

namespace ZYC.Framework.Modules.TilePyramid.Commands;

[Register]
internal class SetMinimapPositionCommand : CommandBase<FloatPosition>
{
    public SetMinimapPositionCommand(TilePyramidDocumentState tilePyramidDocument)
    {
        TilePyramidDocument = tilePyramidDocument;
    }

    private TilePyramidDocumentState TilePyramidDocument { get; }

    protected override bool InternalCanExecute(FloatPosition parameter)
    {
        return TilePyramidDocument.FloatingPanels[FloatingPanelIds.Minimap].Position != parameter;
    }

    protected override void InternalExecute(FloatPosition parameter)
    {
        TilePyramidDocument.MoveFloatingPanel(FloatingPanelIds.Minimap, parameter);
    }
}
