using ZYC.CoreToolkit.Extensions.Autofac.Attributes;
using ZYC.Framework.Core.Commands;
using ZYC.Framework.Modules.TilePyramid.UI;
using ZYC.TilePyramid.Annotations.Wpf.Editing;

namespace ZYC.Framework.Modules.TilePyramid.Commands;

[Register]
internal class AnnotationToolCommand : CommandBase<AnnotationTool>
{
    public AnnotationToolCommand(TilePyramidDocumentView tilePyramidDocumentView)
    {
        TilePyramidDocumentView = tilePyramidDocumentView;
    }

    private TilePyramidDocumentView TilePyramidDocumentView { get; }


    protected override void InternalExecute(AnnotationTool parameter)
    {
        TilePyramidDocumentView.AnnotationCreationRequests.OnNext(parameter);
    }

    protected override bool InternalCanExecute(AnnotationTool parameter)
    {
        return TilePyramidDocumentView.CanCreateAnnotation;
    }
}