using ZYC.CoreToolkit.Extensions.Autofac.Attributes;
using ZYC.Framework.Abstractions.MainMenu;
using ZYC.Framework.Modules.TilePyramid.Abstractions;
using ZYC.Framework.Modules.TilePyramid.Commands;

namespace ZYC.Framework.Modules.TilePyramid.MainMenu;

[RegisterSingleInstance]
internal class OpenMainMenuItem : MainMenuItem
{
    public OpenMainMenuItem(SelectFileCommand selectFileCommand)
    {
        Info = new MenuItemInfo
        {
            Title = $"Open {TilePyramidSelectFileMainMenuItem.MenuTitle}",
            Icon = TilePyramidModuleConstants.Icon
        };

        Command = selectFileCommand;
    }
}