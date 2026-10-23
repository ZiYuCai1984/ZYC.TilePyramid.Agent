using ZYC.TilePyramid.Annotations;

namespace ZYC.Framework.Modules.TilePyramid;

internal partial class TilePyramidAgentService
{
    public Task<Annotation> UpdateAnnotationAsync(Guid documentId, Guid annotationId, string? label = null,
        uint? strokeArgb = null, string? icon = null, string? comments = null, string? layerId = null,
        CancellationToken cancellationToken = default)
    {
        if (annotationId == Guid.Empty)
        {
            throw new ArgumentException("A non-empty annotation ID is required.", nameof(annotationId));
        }

        return OnUIAsync(() =>
        {
            var tab = GetTab(documentId);
            EnsureDocumentWriteAllowed(tab);
            var annotations = tab.AnnotationImage.Annotations;
            var annotation = annotations.Find(annotationId)
                             ?? throw new KeyNotFoundException($"Annotation '{annotationId}' was not found.");
            var targetLayerId = layerId ?? annotation.LayerId;
            RequireUnlockedLayer(tab, annotation.LayerId);
            RequireUnlockedLayer(tab, targetLayerId);

            // Validate both layers before changing any property or registering an implicit layer.
            tab.DocumentState.AnnotationLayers.GetOrCreate(targetLayerId);
            using (annotations.BeginUpdate())
            {
                if (label is not null)
                {
                    annotation.Label = label.Trim();
                }

                if (strokeArgb.HasValue)
                {
                    annotation.StrokeArgb = strokeArgb.Value;
                }

                if (icon is not null)
                {
                    annotation.Icon = icon.Trim();
                }

                if (comments is not null)
                {
                    annotation.Comments = comments;
                }

                annotation.LayerId = targetLayerId;
            }

            return CloneAnnotation(annotation);
        }, cancellationToken);
    }
}
