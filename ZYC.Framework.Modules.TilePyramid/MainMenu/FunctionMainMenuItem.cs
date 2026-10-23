using Autofac;
using ZYC.CoreToolkit.Extensions.Autofac.Attributes;
using ZYC.Framework.Abstractions.MainMenu;
using ZYC.Framework.Core;
using ZYC.Framework.Modules.TilePyramid.Abstractions;

namespace ZYC.Framework.Modules.TilePyramid.MainMenu;

[RegisterSingleInstance]
internal class FunctionMainMenuItem : MainMenuItem
{
    public FunctionMainMenuItem(ILifetimeScope lifetimeScope)
    {
        Info = new MenuItemInfo
        {
            Title = "Function Plot",
            Icon = TilePyramidSourceKindTools.GetIcon(TilePyramidSourceKind.Function)
        };

        Command = lifetimeScope.CreateNavigateCommand(
            TilePyramidModuleConstants.CreateDocumentUri(TilePyramidSourceKind.Function));
    }
}
