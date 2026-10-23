using System.Windows;
using ZYC.CoreToolkit.Extensions.Autofac.Attributes;
using ZYC.Framework.Core.Commands;
using ZYC.TilePyramid.Wpf;

namespace ZYC.Framework.Modules.TilePyramid.Commands;

[Register]
internal class ZoomPresetCommand : CommandBase<double>
{
    public ZoomPresetCommand(TilePyramidView tilePyramidView)
    {
        TilePyramidView = tilePyramidView;
    }

    private TilePyramidView TilePyramidView { get; }

    protected override void InternalExecute(double factor)
    {
        var scale = TilePyramidView.Metrics.ReferenceScale * factor;
        TilePyramidView.ZoomAt(
            scale,
            new Point(TilePyramidView.ActualWidth / 2, TilePyramidView.ActualHeight / 2));
    }
}
