using ZYC.CoreToolkit.Extensions.Autofac.Attributes;
using ZYC.Framework.Core.Commands;
using ZYC.Framework.Modules.TilePyramid.Abstractions;

namespace ZYC.Framework.Modules.TilePyramid.Commands;

[Register]
internal class SetMinimapSizeCommand : CommandBase<double>
{
    public SetMinimapSizeCommand(TilePyramidDocumentState documentState)
    {
        DocumentState = documentState;
    }

    private TilePyramidDocumentState DocumentState { get; }

    protected override bool InternalCanExecute(double parameter)
    {
        return parameter is 1 or 2;
    }

    protected override void InternalExecute(double parameter)
    {
        if (!InternalCanExecute(parameter))
        {
            return;
        }

        var panel = DocumentState.FloatingPanels[FloatingPanelIds.Minimap];
        panel.Width = TilePyramidDocumentState.DefaultMinimapWidth * parameter;
        panel.Height = TilePyramidDocumentState.DefaultMinimapHeight * parameter;
    }
}
