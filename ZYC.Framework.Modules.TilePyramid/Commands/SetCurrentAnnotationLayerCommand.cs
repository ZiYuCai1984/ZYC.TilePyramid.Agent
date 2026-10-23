using ZYC.CoreToolkit.Extensions.Autofac.Attributes;
using ZYC.Framework.Core.Commands;
using ZYC.Framework.Modules.TilePyramid.UI;
using ZYC.Framework.Modules.TilePyramid.UI.Annotations;

namespace ZYC.Framework.Modules.TilePyramid.Commands;

[Register]
internal class SetCurrentAnnotationLayerCommand : CommandBase<AnnotationLayerItem>
{
    public SetCurrentAnnotationLayerCommand(TilePyramidDocumentView view)
    {
        View = view;
    }

    private TilePyramidDocumentView View { get; }

    protected override void InternalExecute(AnnotationLayerItem parameter)
    {
        View.SelectedAnnotationLayer = parameter;
    }

    protected override bool InternalCanExecute(AnnotationLayerItem parameter)
    {
        return View.SelectedAnnotationLayer != parameter;
    }
}