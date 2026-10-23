using Autofac;
using ZYC.CoreToolkit.Extensions.Autofac.Attributes;
using ZYC.Framework.Abstractions;
using ZYC.Framework.Core.Commands;
using ZYC.Framework.Modules.TilePyramid.UI;
using ZYC.Framework.Modules.TilePyramid.UI.Annotations;

namespace ZYC.Framework.Modules.TilePyramid.Commands;

[Register]
internal class NewAnnotationLayerDialogCommand : CommandBase
{
    public NewAnnotationLayerDialogCommand(
        TilePyramidDocumentView tilePyramidDocumentView,
        IDialogManager dialogManager)
    {
        TilePyramidDocumentView = tilePyramidDocumentView;
        DialogManager = dialogManager;
    }

    private TilePyramidDocumentView TilePyramidDocumentView { get; }

    private IDialogManager DialogManager { get; }

    public override bool CanExecute(object? parameter)
    {
        return TilePyramidDocumentView.CanStartAnnotationEdit;
    }

    protected override void InternalExecute(object? parameter)
    {
        if (!CanExecute(parameter))
        {
            return;
        }

        DialogManager.Show<EditOrNewAnnotationLayerDialog>(
            new TypedParameter(typeof(TilePyramidDocumentView), TilePyramidDocumentView),
            new TypedParameter(typeof(EditOrNewAnnotationLayerRequest), new EditOrNewAnnotationLayerRequest
            {
                Title = "New Layer",
                LayerName = DefaultNameGenerator.GetNext("Layer",
                    TilePyramidDocumentView.DocumentState.AnnotationLayers.Definitions.Select(layer => layer.Name))
            }));
    }
}
