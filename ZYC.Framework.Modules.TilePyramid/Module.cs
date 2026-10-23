using Autofac;
using ZYC.CoreToolkit;
using ZYC.CoreToolkit.Extensions.Autofac;
using ZYC.Framework.Abstractions.MainMenu;
using ZYC.Framework.Core;
using ZYC.Framework.Modules.TilePyramid.Abstractions;
using ZYC.Framework.Modules.TilePyramid.MainMenu;
using ZYC.Framework.Modules.TilePyramid.StatusBar;

namespace ZYC.Framework.Modules.TilePyramid;

internal class Module : ModuleBase
{
    public override string Icon => TilePyramidModuleConstants.Icon;

    public override Task LoadAsync(ILifetimeScope lifetimeScope)
    {
        //TODO-zyc DebuggerTools.Attach()
        //DebuggerTools.Attach();

        lifetimeScope.RegisterTabItemFactory<TilePyramidTabItemFactory>();

        lifetimeScope.Resolve<IFileOpenMainMenuItemsProvider>()
            .RegisterSubItem<TilePyramidSelectFileMainMenuItem>();

        lifetimeScope.RegisterRootMainMenuItem<ITilePyramidMainMenuItemsProvider>();

        lifetimeScope.RegisterDefaultStatusBarItem<TilePyramidDocumentStatusBarItem>();
        lifetimeScope.RegisterDefaultStatusBarItem<TilePyramidAgentTaskStatusBarItem>();

        return Task.CompletedTask;
    }


    public override Task UnloadAsync(ILifetimeScope lifetimeScope)
    {
        if (lifetimeScope.TryResolve<TilePyramidAgentTasks>(out var agentTasks))
        {
            agentTasks.Dispose();
        }

        if (lifetimeScope.TryResolve<TilePyramidDocumentStateStore>(out var stateStore))
        {
            stateStore.Dispose();
        }
        return Task.CompletedTask;
    }
}
