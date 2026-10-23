using ZYC.CoreToolkit.Extensions.Autofac.Attributes;
using ZYC.Framework.Core.Commands;
using ZYC.Framework.Modules.TilePyramid.UI;

namespace ZYC.Framework.Modules.TilePyramid.Commands;

[Register]
internal class CenterOnCommand : CommandBase
{
    public CenterOnCommand(TilePyramidDocumentView tilePyramidDocumentView)
    {
        TilePyramidDocumentView = tilePyramidDocumentView;
    }

    private TilePyramidDocumentView TilePyramidDocumentView { get; }


    protected override void InternalExecute(object? parameter)
    {
        TilePyramidDocumentView.CenterOn();
    }
}