using ZYC.CoreToolkit.Extensions.Autofac.Attributes;
using ZYC.Framework.Core.Commands;
using ZYC.Framework.Modules.TilePyramid.Abstractions;
using ZYC.Framework.Modules.TilePyramid.UI;

namespace ZYC.Framework.Modules.TilePyramid.Commands;

[Register]
internal class RotateLabelImageCommand : CommandBase<TilePyramidDocumentState>
{
    public RotateLabelImageCommand(TilePyramidDocumentView tilePyramidDocumentView)
    {
        TilePyramidDocumentView = tilePyramidDocumentView;
    }

    private TilePyramidDocumentView TilePyramidDocumentView { get; }

    protected override bool InternalCanExecute(TilePyramidDocumentState parameter)
    {
        return TilePyramidDocumentView.HasLabelImage;
    }


    protected override void InternalExecute(TilePyramidDocumentState state)
    {
        state.LabelImageRotation = (state.LabelImageRotation + 90) % 360;
    }
}