using ZYC.CoreToolkit.Extensions.Autofac.Attributes;
using ZYC.Framework.Core.Commands;
using ZYC.Framework.Modules.TilePyramid.UI;
using ZYC.Framework.Modules.TilePyramid.UI.Annotations;
using ZYC.TilePyramid.Annotations;

namespace ZYC.Framework.Modules.TilePyramid.Commands;

[Register]
internal class EditAnnotationPositionCommand : CommandBase<Annotation>
{
    public EditAnnotationPositionCommand(TilePyramidDocumentView tilePyramidDocumentView)
    {
        TilePyramidDocumentView = tilePyramidDocumentView;
    }

    private TilePyramidDocumentView TilePyramidDocumentView { get; }

    protected override bool InternalCanExecute(Annotation? parameter)
    {
        return parameter is not null
               && TilePyramidDocumentView.CanEditAnnotation(parameter, AnnotationEditingOperation.Position);
    }

    protected override void InternalExecute(Annotation parameter)
    {
        TilePyramidDocumentView.StartAnnotationEdit(parameter, AnnotationEditingOperation.Position);
    }
}