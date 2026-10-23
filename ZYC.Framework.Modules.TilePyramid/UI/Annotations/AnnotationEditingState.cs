using ZYC.TilePyramid.Annotations;
using ZYC.TilePyramid.Annotations.Wpf.Editing;

namespace ZYC.Framework.Modules.TilePyramid.UI.Annotations;

internal enum AnnotationEditingOperation
{
    Create,
    Position,
    Information
}

internal enum AnnotationEditingPhase
{
    Idle,
    Ready,
    Drawing,
    SelectionReady,
    EditingProperties,
    Committing
}

internal record AnnotationEditingState(
    AnnotationEditingPhase Phase,
    AnnotationEditingOperation Operation = AnnotationEditingOperation.Create,
    AnnotationTool Tool = AnnotationTool.Navigate,
    Annotation? Target = null,
    AnnotationShape? Shape = null,
    Annotation? Result = null,
    bool HasValidDraft = false)
{
    public bool IsActive => Phase != AnnotationEditingPhase.Idle;
    public bool CanConfirm => Phase == AnnotationEditingPhase.SelectionReady && Shape is not null;
    public bool CanCompleteDraft => Operation == AnnotationEditingOperation.Create
                                    && Tool == AnnotationTool.Polygon
                                    && Phase == AnnotationEditingPhase.Drawing && HasValidDraft;
}
