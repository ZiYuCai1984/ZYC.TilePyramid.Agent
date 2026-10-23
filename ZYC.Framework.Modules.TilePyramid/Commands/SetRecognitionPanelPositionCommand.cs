using ZYC.CoreToolkit.Extensions.Autofac.Attributes;
using ZYC.Framework.Core.Commands;
using ZYC.Framework.Modules.TilePyramid.Abstractions;
using ZYC.TilePyramid.Core;

namespace ZYC.Framework.Modules.TilePyramid.Commands;

[Register]
internal class SetRecognitionPanelPositionCommand : CommandBase<FloatPosition>
{
    public SetRecognitionPanelPositionCommand(TilePyramidDocumentState documentState)
    {
        DocumentState = documentState;
    }

    private TilePyramidDocumentState DocumentState { get; }

    protected override bool InternalCanExecute(FloatPosition parameter)
    {
        return DocumentState.FloatingPanels[FloatingPanelIds.Recognition].Position != parameter;
    }

    protected override void InternalExecute(FloatPosition parameter)
    {
        DocumentState.MoveFloatingPanel(FloatingPanelIds.Recognition, parameter);
    }
}