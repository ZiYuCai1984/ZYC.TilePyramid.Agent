using Autofac;
using ZYC.CoreToolkit.Extensions.Autofac.Attributes;
using ZYC.Framework.Abstractions.MainMenu;
using ZYC.Framework.Core.Commands;
using ZYC.Framework.Core.Menu;
using ZYC.Framework.Modules.TilePyramid.Abstractions;

namespace ZYC.Framework.Modules.TilePyramid.MainMenu;

[RegisterSingleInstance]
internal class SetShowDiagnosticsMainMenuItemsProvider : MainMenuItemsProvider
{
    public SetShowDiagnosticsMainMenuItemsProvider(ILifetimeScope lifetimeScope) : base(lifetimeScope)
    {
        Info = new MenuItemInfo
        {
            Title = "Show Diagnostics",
            Icon = null
        };

        var config = lifetimeScope.Resolve<TilePyramidConfig>();

        RegisterSubItem(new MainMenuItem("Enable", null,
            new RelayCommand(_ => !config.ShowDiagnostics, _ => { config.ShowDiagnostics = true; })));
        RegisterSubItem(new MainMenuItem("Disable", null,
            new RelayCommand(_ => config.ShowDiagnostics, _ => { config.ShowDiagnostics = false; })));
    }

    public override MenuItemInfo Info { get; }
}