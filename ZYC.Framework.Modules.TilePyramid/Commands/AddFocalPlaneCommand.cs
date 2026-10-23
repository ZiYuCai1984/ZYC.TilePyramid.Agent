using ZYC.CoreToolkit.Extensions.Autofac.Attributes;
using ZYC.Framework.Core.Commands;
using ZYC.Framework.Modules.TilePyramid.UI;

namespace ZYC.Framework.Modules.TilePyramid.Commands;

[Register]
internal class AddFocalPlaneIndexCommand : CommandBase
{
    public AddFocalPlaneIndexCommand(TilePyramidDocumentView tilePyramidDocumentView)
    {
        TilePyramidDocumentView = tilePyramidDocumentView;
    }

    private TilePyramidDocumentView TilePyramidDocumentView { get; }

    protected override void InternalExecute(object? parameter)
    {
        ++TilePyramidDocumentView.MainView.FocalPlaneIndex;
    }

    public override bool CanExecute(object? parameter)
    {
        return TilePyramidDocumentView.CanIncreaseFocalPlane;
    }
}