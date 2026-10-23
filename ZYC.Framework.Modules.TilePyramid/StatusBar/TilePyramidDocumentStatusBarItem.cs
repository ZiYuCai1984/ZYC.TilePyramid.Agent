using Autofac;
using ZYC.CoreToolkit.Extensions.Autofac.Attributes;
using ZYC.Framework.Abstractions.StatusBar;

namespace ZYC.Framework.Modules.TilePyramid.StatusBar;

[RegisterSingleInstance]
internal class TilePyramidDocumentStatusBarItem : IStatusBarItem
{
    private object? _view;

    public TilePyramidDocumentStatusBarItem(ILifetimeScope lifetimeScope)
    {
        LifetimeScope = lifetimeScope;
    }

    public ILifetimeScope LifetimeScope { get; }

    public object View => _view ??= LifetimeScope.Resolve<TilePyramidDocumentStatusBarItemView>();

    public StatusBarSection Section => StatusBarSection.Left;
}