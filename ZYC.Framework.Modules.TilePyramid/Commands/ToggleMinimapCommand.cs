using ZYC.CoreToolkit.Extensions.Autofac.Attributes;
using ZYC.Framework.Core.Commands;
using ZYC.Framework.Modules.TilePyramid.Abstractions;

namespace ZYC.Framework.Modules.TilePyramid.Commands;

[RegisterSingleInstance]
internal class ToggleMinimapCommand : CommandBase<TilePyramidDocumentState>
{
    protected override void InternalExecute(TilePyramidDocumentState parameter)
    {
        var panel = parameter.FloatingPanels[FloatingPanelIds.Minimap];
        panel.IsVisible = !panel.IsVisible;
    }
}
