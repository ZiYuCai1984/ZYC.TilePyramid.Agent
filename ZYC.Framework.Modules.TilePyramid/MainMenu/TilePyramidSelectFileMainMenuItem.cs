using ZYC.CoreToolkit.Extensions.Autofac.Attributes;
using ZYC.Framework.Abstractions.MainMenu;
using ZYC.Framework.Modules.TilePyramid.Abstractions;
using ZYC.Framework.Modules.TilePyramid.Commands;

namespace ZYC.Framework.Modules.TilePyramid.MainMenu;

[RegisterSingleInstance]
internal class TilePyramidSelectFileMainMenuItem : MainMenuItem
{
    public TilePyramidSelectFileMainMenuItem(SelectFileCommand selectFileCommand)
    {
        Info = new MenuItemInfo
        {
            Title = MenuTitle,
            Icon = TilePyramidModuleConstants.Icon
        };

        Command = selectFileCommand;
    }


    public static string MenuTitle => "TilePyramid File";
}