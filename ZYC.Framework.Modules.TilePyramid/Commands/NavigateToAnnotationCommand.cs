using ZYC.CoreToolkit.Extensions.Autofac.Attributes;
using ZYC.Framework.Core.Commands;
using ZYC.Framework.Modules.TilePyramid.UI;
using ZYC.Framework.Modules.TilePyramid.UI.RecognitionReview;

namespace ZYC.Framework.Modules.TilePyramid.Commands;

[Register]
internal class NavigateToAnnotationCommand : CommandBase<RecognitionReviewItem>
{
    public NavigateToAnnotationCommand(TilePyramidDocumentView view)
    {
        View = view;
    }

    private TilePyramidDocumentView View { get; }

    protected override bool InternalCanExecute(RecognitionReviewItem? parameter)
    {
        return parameter is not null && View.CanNavigateToAnnotation(parameter);
    }


    protected override void InternalExecute(RecognitionReviewItem parameter)
    {
        View.NavigateToAnnotation(parameter);
    }
}