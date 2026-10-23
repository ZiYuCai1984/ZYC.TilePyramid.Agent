using Autofac;
using ZYC.Framework.Modules.TilePyramid.Commands;

namespace ZYC.Framework.Modules.TilePyramid.UI;

internal partial class TilePyramidDocumentView
{
    private AnnotationToolCommand? _annotationToolCommand;

    public AnnotationToolCommand AnnotationToolCommand => _annotationToolCommand ??=
        LifetimeScope.Resolve<AnnotationToolCommand>(new TypedParameter(typeof(TilePyramidDocumentView),
            this));
}