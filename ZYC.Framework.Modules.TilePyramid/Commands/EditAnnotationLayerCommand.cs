using Autofac;
using ZYC.CoreToolkit.Extensions.Autofac.Attributes;
using ZYC.Framework.Abstractions;
using ZYC.Framework.Core.Commands;
using ZYC.Framework.Modules.TilePyramid.UI;
using ZYC.Framework.Modules.TilePyramid.UI.Annotations;

namespace ZYC.Framework.Modules.TilePyramid.Commands;

[Register]
internal class EditAnnotationLayerCommand : CommandBase<AnnotationLayerItem>
{
    public EditAnnotationLayerCommand(
        TilePyramidDocumentView tilePyramidDocumentView,
        IDialogManager dialogManager)
    {
        TilePyramidDocumentView = tilePyramidDocumentView;
        DialogManager = dialogManager;
    }

    private TilePyramidDocumentView TilePyramidDocumentView { get; }

    private IDialogManager DialogManager { get; }

    protected override bool InternalCanExecute(AnnotationLayerItem? parameter)
    {
        return TilePyramidDocumentView.CanModifyAnnotationLayer(parameter);
    }

    protected override void InternalExecute(AnnotationLayerItem parameter)
    {
        if (!InternalCanExecute(parameter))
        {
            return;
        }

        DialogManager.Show<EditOrNewAnnotationLayerDialog>(
            new TypedParameter(typeof(TilePyramidDocumentView), TilePyramidDocumentView),
            new TypedParameter(typeof(EditOrNewAnnotationLayerRequest), new EditOrNewAnnotationLayerRequest
            {
                Title = "Rename Layer",
                LayerName = parameter.Name,
                TargetLayer = parameter
            }));
    }
}