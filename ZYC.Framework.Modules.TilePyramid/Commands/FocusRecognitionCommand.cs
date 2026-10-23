using ZYC.CoreToolkit.Extensions.Autofac.Attributes;
using ZYC.Framework.Core.Commands;
using ZYC.Framework.Modules.TilePyramid.UI;
using ZYC.Framework.Modules.TilePyramid.UI.Annotations;
using ZYC.Framework.Modules.TilePyramid.UI.RecognitionReview;

namespace ZYC.Framework.Modules.TilePyramid.Commands;

[Register]
internal class FocusRecognitionCommand(TilePyramidDocumentView view) : CommandBase<RecognitionReviewItem>
{
    protected override bool InternalCanExecute(RecognitionReviewItem? parameter)
    {
        return parameter is not null && view.CanFocusRecognition(parameter);
    }

    protected override void InternalExecute(RecognitionReviewItem parameter)
    {
        view.FocusRecognition(parameter);
    }
}
