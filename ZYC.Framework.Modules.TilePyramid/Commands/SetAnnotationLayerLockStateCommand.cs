using ZYC.CoreToolkit.Extensions.Autofac.Attributes;
using ZYC.Framework.Core.Commands;
using ZYC.Framework.Modules.TilePyramid.UI;
using ZYC.Framework.Modules.TilePyramid.UI.Annotations;

namespace ZYC.Framework.Modules.TilePyramid.Commands;

[Register]
internal class SetAnnotationLayerLockStateCommand : CommandBase<AnnotationLayerItem>
{
    public SetAnnotationLayerLockStateCommand(TilePyramidDocumentView view)
    {
        View = view;
    }

    private TilePyramidDocumentView View { get; }

    protected override bool InternalCanExecute(AnnotationLayerItem? parameter)
    {
        return View.CanModifyAnnotationLayer(parameter);
    }

    protected override void InternalExecute(AnnotationLayerItem parameter)
    {
        if (InternalCanExecute(parameter))
        {
            View.SetAnnotationLayerLockState(parameter, !parameter.IsLocked);
        }
    }
}