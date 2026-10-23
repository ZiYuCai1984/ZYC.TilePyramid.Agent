using Autofac;
using ZYC.CoreToolkit.Extensions.Autofac.Attributes;
using ZYC.Framework.Abstractions.StatusBar;

namespace ZYC.Framework.Modules.TilePyramid.StatusBar;

[RegisterSingleInstance]
internal class TilePyramidAgentTaskStatusBarItem : IStatusBarItem
{
    private object? _view;

    public TilePyramidAgentTaskStatusBarItem(ILifetimeScope lifetimeScope)
    {
        LifetimeScope = lifetimeScope;
    }

    private ILifetimeScope LifetimeScope { get; }
    public object View => _view ??= LifetimeScope.Resolve<TilePyramidAgentTaskStatusBarItemView>();
    public StatusBarSection Section => StatusBarSection.Right;

    public int Order => -1;
}