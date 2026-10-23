using Autofac;
using ZYC.CoreToolkit.Extensions.Autofac.Attributes;
using ZYC.Framework.Abstractions.MainMenu;
using ZYC.Framework.Core.Menu;
using ZYC.Framework.Modules.TilePyramid.Abstractions;

namespace ZYC.Framework.Modules.TilePyramid.MainMenu;

[RegisterSingleInstanceAs(typeof(ITilePyramidMainMenuItemsProvider))]
internal class TilePyramidMainMenuItemsProvider : MainMenuItemsProvider, ITilePyramidMainMenuItemsProvider
{
    public TilePyramidMainMenuItemsProvider(ILifetimeScope lifetimeScope) : base(lifetimeScope)
    {
        Info = new MenuItemInfo
        {
            Title = TilePyramidModuleConstants.Title,
            Priority = MainMenuPriority.Extensions + 5
        };

        RegisterSubItem<OpenMainMenuItem>();
        RegisterSubItem<MandelbrotMainMenuItem>();
        //RegisterSubItem<FunctionMainMenuItem>();
        RegisterSubItem<SetShowDiagnosticsMainMenuItemsProvider>();
    }

    public override MenuItemInfo Info { get; }
}
