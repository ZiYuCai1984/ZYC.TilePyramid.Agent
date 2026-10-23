using ZYC.CoreToolkit.Extensions.Autofac.Attributes;
using ZYC.Framework.Core.Commands;
using ZYC.Framework.Modules.TilePyramid.UI;
using ZYC.TilePyramid.Annotations;

namespace ZYC.Framework.Modules.TilePyramid.Commands;

[Register]
internal class FocusAnnotationCommand : CommandBase<Annotation>
{
    public FocusAnnotationCommand(TilePyramidDocumentView view)
    {
        View = view;
    }

    private TilePyramidDocumentView View { get; }

    protected override bool InternalCanExecute(Annotation? parameter)
    {
        return parameter is not null && View.CanFocusAnnotation(parameter);
    }

    protected override void InternalExecute(Annotation parameter)
    {
        View.FocusAnnotation(parameter);
    }
}
