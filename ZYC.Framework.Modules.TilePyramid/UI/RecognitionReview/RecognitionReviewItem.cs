using System.ComponentModel;
using ZYC.TilePyramid.Annotations;

namespace ZYC.Framework.Modules.TilePyramid.UI.RecognitionReview;

/// <summary>A stable review row backed by a recognition result throughout its lifecycle.</summary>
internal class RecognitionReviewItem : ObservableObject, IDisposable
{
    private readonly AnnotationCollection<Annotation> _annotations;
    private readonly AnnotationLayerSettings _layers;
    private Annotation? _annotation;
    private AnnotationLayerDefinition? _layer;

    public RecognitionReviewItem(RecognitionFeature feature, RecognitionBatch batch,
        AnnotationCollection<Annotation> annotations, AnnotationLayerSettings layers)
    {
        Feature = feature;
        Batch = batch;
        _annotations = annotations;
        _layers = layers;
        _annotation = FindAnnotation();
        _layer = _layers.Definitions.Find(Feature.LayerId);
        Feature.PropertyChanged += OnFeatureChanged;
    }

    public RecognitionFeature Feature { get; }
    public RecognitionBatch Batch { get; }
    public Annotation? Annotation => _annotation;
    public AnnotationLayerDefinition? Layer => _layer;
    public (Guid BatchId, Guid Id) Key => (Batch.Id, Feature.Id);
    public Guid BatchId => Batch.Id;
    public ReviewDecision Decision => Feature.Decision;
    public bool ShowAcceptAction => Feature.Decision == ReviewDecision.Pending;
    public bool ShowRejectAction => Feature.Decision == ReviewDecision.Pending;
    public bool ShowResetAction => Feature.Decision is ReviewDecision.Accepted or ReviewDecision.Rejected;

    public bool ShowCreateAnnotationAction =>
        Feature.Decision == ReviewDecision.Accepted && Feature.AnnotationId is null;

    public int FocalPlaneNumber => Batch.FocalPlaneIndex + 1;

    public bool IsReviewLocked => _layers.IsLocked(Feature.LayerId)
                                  || (FindAnnotation() is { } annotation && _layers.IsLocked(annotation.LayerId));

    public void Dispose()
    {
        Feature.PropertyChanged -= OnFeatureChanged;
    }

    public void RefreshReferences()
    {
        Set(ref _annotation, FindAnnotation(), nameof(Annotation));
        Set(ref _layer, _layers.Definitions.Find(Feature.LayerId), nameof(Layer));
    }

    private Annotation? FindAnnotation() => Feature.AnnotationId is { } id ? _annotations.Find(id) : null;

    private void OnFeatureChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == nameof(RecognitionFeature.Decision))
        {
            OnPropertyChanged(nameof(Decision));
            OnPropertyChanged(nameof(ShowAcceptAction));
            OnPropertyChanged(nameof(ShowRejectAction));
            OnPropertyChanged(nameof(ShowResetAction));
        }

        if (string.IsNullOrEmpty(e.PropertyName)
            || e.PropertyName is nameof(RecognitionFeature.Decision) or nameof(RecognitionFeature.AnnotationId))
        {
            OnPropertyChanged(nameof(ShowCreateAnnotationAction));
        }

        if (string.IsNullOrEmpty(e.PropertyName)
            || e.PropertyName is nameof(RecognitionFeature.AnnotationId) or nameof(RecognitionFeature.LayerId))
        {
            RefreshReferences();
        }
    }
}
