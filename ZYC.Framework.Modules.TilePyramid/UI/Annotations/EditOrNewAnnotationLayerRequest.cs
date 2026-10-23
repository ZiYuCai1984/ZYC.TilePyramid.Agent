namespace ZYC.Framework.Modules.TilePyramid.UI.Annotations;

internal class EditOrNewAnnotationLayerRequest
{
    public string Title { get; set; } = "";

    public string LayerName { get; set; } = "";

    public AnnotationLayerItem? TargetLayer { get; init; }

    public bool IsEdit => TargetLayer is not null;
}