using ZYC.CoreToolkit.Extensions.Autofac.Attributes;
using ZYC.Framework.Core.Commands;
using ZYC.Framework.Modules.TilePyramid.UI;
using ZYC.Framework.Modules.TilePyramid.UI.Annotations;

namespace ZYC.Framework.Modules.TilePyramid.Commands;

[Register]
internal class MoveUpAnnotationLayerCommand : CommandBase<AnnotationLayerItem>
{
    public MoveUpAnnotationLayerCommand(TilePyramidDocumentView view)
    {
        View = view;
    }

    private TilePyramidDocumentView View { get; }

    protected override bool InternalCanExecute(AnnotationLayerItem? parameter)
    {
        return View.CanMoveAnnotationLayer(parameter, true);
    }

    protected override void InternalExecute(AnnotationLayerItem parameter)
    {
        View.MoveAnnotationLayer(parameter, true);
    }
}