using ZYC.CoreToolkit.Extensions.Autofac.Attributes;
using ZYC.Framework.Core.Commands;
using ZYC.Framework.Modules.TilePyramid.UI;
using ZYC.Framework.Modules.TilePyramid.UI.Annotations;

namespace ZYC.Framework.Modules.TilePyramid.Commands;

[Register]
internal class DeleteAnnotationLayerCommand : CommandBase<AnnotationLayerItem>
{
    public DeleteAnnotationLayerCommand(TilePyramidDocumentView tilePyramidDocumentView)
    {
        TilePyramidDocumentView = tilePyramidDocumentView;
    }

    private TilePyramidDocumentView TilePyramidDocumentView { get; }


    protected override void InternalExecute(AnnotationLayerItem parameter)
    {
        TilePyramidDocumentView.DeleteAnnotationLayer(parameter);
    }

    protected override bool InternalCanExecute(AnnotationLayerItem? parameter)
    {
        return TilePyramidDocumentView.CanDeleteAnnotationLayerItem(parameter);
    }
}