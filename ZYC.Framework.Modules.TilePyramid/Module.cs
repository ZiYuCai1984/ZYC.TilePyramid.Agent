using System.IO;
using Autofac;
using ZYC.CoreToolkit.Extensions.Autofac;
using ZYC.CoreToolkit.Extensions.Settings;
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

    public override Task RegisterAsync(ContainerBuilder builder)
    {
        try
        {
            var assemblyDirectory = Path.GetDirectoryName(
                typeof(Module).Assembly.Location);

            if (string.IsNullOrEmpty(assemblyDirectory))
            {
                throw new InvalidOperationException(
                    $"Cannot resolve assembly directory for {typeof(Module).Assembly.FullName}.");
            }

            var result = SettingsTools.GetFromFolderGeneric(
                assemblyDirectory, typeof(TilePyramidLanguageResourcesConfig));
            builder.RegisterConfigR(result);
        }
        catch
        {
            //ignore
        }


        return base.RegisterAsync(builder);
    }
}