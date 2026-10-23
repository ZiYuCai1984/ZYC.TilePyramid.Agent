using System.Windows;
using ZYC.CoreToolkit.Extensions.Autofac.Attributes;
using ZYC.Framework.Core.Commands;
using ZYC.TilePyramid.Wpf;

namespace ZYC.Framework.Modules.TilePyramid.Commands;

[Register]
internal class ZoomCommand : CommandBase<double>
{
    public ZoomCommand(TilePyramidView tilePyramidView)
    {
        TilePyramidView = tilePyramidView;
    }

    private TilePyramidView TilePyramidView { get; }

    protected override void InternalExecute(double parameter)
    {
        TilePyramidView.ZoomBy(
            parameter,
            new Point(TilePyramidView.ActualWidth / 2, TilePyramidView.ActualHeight / 2));
    }

    protected override bool InternalCanExecute(double parameter)
    {
        return TilePyramidView.IsZoomEnabled;
    }
}