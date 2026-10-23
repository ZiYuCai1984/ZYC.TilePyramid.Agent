using Autofac;
using ZYC.CoreToolkit.Extensions.Autofac.Attributes;
using ZYC.Framework.Abstractions.MainMenu;
using ZYC.Framework.Core;
using ZYC.Framework.Modules.TilePyramid.Abstractions;

namespace ZYC.Framework.Modules.TilePyramid.MainMenu;

[RegisterSingleInstance]
internal class MandelbrotMainMenuItem : MainMenuItem
{
    public MandelbrotMainMenuItem(ILifetimeScope lifetimeScope)
    {
        Info = new MenuItemInfo
        {
            Title = "Mandelbrot",
            Icon = TilePyramidSourceKindTools.GetIcon(TilePyramidSourceKind.Mandelbrot)
        };

        Command = lifetimeScope.CreateNavigateCommand(
            TilePyramidModuleConstants.MandelbrotUri());
    }
}
