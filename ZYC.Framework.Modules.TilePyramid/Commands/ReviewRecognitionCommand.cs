using ZYC.CoreToolkit.Extensions.Autofac.Attributes;
using ZYC.Framework.Core.Commands;
using ZYC.Framework.Modules.TilePyramid.UI;
using ZYC.Framework.Modules.TilePyramid.UI.Annotations;
using ZYC.Framework.Modules.TilePyramid.UI.RecognitionReview;

namespace ZYC.Framework.Modules.TilePyramid.Commands;

[Register]
internal class ReviewRecognitionCommand : AsyncCommandBase<RecognitionReviewItem>
{
    public ReviewRecognitionCommand(TilePyramidDocumentView view, RecognitionReviewAction action)
    {
        View = view;
        Action = action;
    }

    private TilePyramidDocumentView View { get; }
    private RecognitionReviewAction Action { get; }

    protected override bool InternalCanExecute(RecognitionReviewItem? parameter)
    {
        return parameter is not null && View.CanReviewRecognition(parameter, Action);
    }


    protected override Task InternalExecuteAsync(RecognitionReviewItem parameter)
    {
        return View.ReviewRecognitionAsync(parameter, Action);
    }
}
