using ZYC.CoreToolkit.Extensions.Autofac.Attributes;
using ZYC.Framework.Core.Commands;
using ZYC.Framework.Modules.TilePyramid.UI;
using ZYC.TilePyramid.Annotations;

namespace ZYC.Framework.Modules.TilePyramid.Commands;

[Register]
internal class DeleteAnnotationCommand : CommandBase<Annotation>
{
    public DeleteAnnotationCommand(TilePyramidDocumentView view)
    {
        View = view;
    }

    private TilePyramidDocumentView View { get; }


    protected override void InternalExecute(Annotation parameter)
    {
        if (View.CanDeleteAnnotation(parameter))
        {
            View.DeleteAnnotation(parameter);
        }
    }

    protected override bool InternalCanExecute(Annotation? parameter)
    {
        return parameter is not null && View.CanDeleteAnnotation(parameter);
    }

    public override bool CanExecute(object? parameter)
    {
        if (parameter == null)
        {
            return false;
        }

        return base.CanExecute(parameter);
    }
}
