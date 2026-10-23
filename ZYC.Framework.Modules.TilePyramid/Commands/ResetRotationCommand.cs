using ZYC.CoreToolkit.Extensions.Autofac.Attributes;
using ZYC.Framework.Core.Commands;
using ZYC.TilePyramid.Wpf;

namespace ZYC.Framework.Modules.TilePyramid.Commands;

[Register]
internal class ResetRotationCommand : CommandBase
{
    public ResetRotationCommand(TilePyramidView tilePyramidView)
    {
        TilePyramidView = tilePyramidView;
    }

    private TilePyramidView TilePyramidView { get; }


    protected override void InternalExecute(object? parameter)
    {
        TilePyramidView.ResetRotation();
    }
}
