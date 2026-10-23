using ZYC.CoreToolkit.Extensions.Autofac.Attributes;
using ZYC.Framework.Core.Commands;
using ZYC.Framework.Modules.TilePyramid.Abstractions;

namespace ZYC.Framework.Modules.TilePyramid.Commands;

[RegisterSingleInstance]
internal class ToggleAgentTasksPanelCommand : CommandBase<TilePyramidDocumentState>
{
    protected override void InternalExecute(TilePyramidDocumentState parameter)
    {
        parameter.IsAgentTasksPanelOpened = !parameter.IsAgentTasksPanelOpened;
    }
}