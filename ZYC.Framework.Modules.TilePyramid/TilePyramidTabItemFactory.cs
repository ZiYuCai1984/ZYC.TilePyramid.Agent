using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using Autofac;
using ZYC.CoreToolkit.Extensions.Autofac.Attributes;
using ZYC.Framework.Abstractions;
using ZYC.Framework.Abstractions.BusyWindow;
using ZYC.Framework.Abstractions.Event;
using ZYC.Framework.Abstractions.Tab;
using ZYC.Framework.Core;
using ZYC.Framework.Modules.TilePyramid.Abstractions;

namespace ZYC.Framework.Modules.TilePyramid;

[RegisterSingleInstance]
[TabItemRoute(Host = TilePyramidModuleConstants.Host)]
internal class TilePyramidTabItemFactory : TabItemFactoryBase, IDisposable
{
    public TilePyramidTabItemFactory(
        IEventAggregator eventAggregator, 
        IAppBusyWindow appBusyWindow,
        IMainWindow mainWindow)
    {
        IBusyWindowHandler? handler = null;

        eventAggregator.Observe<MainWindowLoadedEvent>()
            .Subscribe(_ =>
            {
                mainWindow.SetIsFrozen(true);
                handler = appBusyWindow.Enqueue();
            }).DisposeWith(CompositeDisposable);


        eventAggregator.Observe<TabItemsPreloadCompletedEvent>()
            .Subscribe(_ =>
            {
                mainWindow.SetIsFrozen(false);
                handler?.Dispose();
            }).DisposeWith(CompositeDisposable);
    }


    private CompositeDisposable CompositeDisposable { get; } = new();

    public override bool IsSingle => false;

    public void Dispose()
    {
        CompositeDisposable.Dispose();
    }

    public override async Task<bool> CheckUriMatchedAsync(Uri uri)
    {
        return uri.IsAbsoluteUri
               && await base.CheckUriMatchedAsync(uri)
               && TilePyramidDocumentRoute.TryParse(uri, out _);
    }

    public override Task<ITabItemInstance> CreateTabItemInstanceAsync(TabItemCreationContext context)
    {
        return Task.FromResult<ITabItemInstance>(context.Resolve<TilePyramidTabItem>(
            new TypedParameter(typeof(MutableTabReference), new MutableTabReference(context.Uri))));
    }
}