using System.Windows.Input;
using Autofac;
using ZYC.Framework.Modules.TilePyramid.Abstractions;
using ZYC.Framework.Modules.TilePyramid.Commands;
using ZYC.Framework.Modules.TilePyramid.UI.RecognitionReview;
using ZYC.TilePyramid.Annotations;

namespace ZYC.Framework.Modules.TilePyramid.UI;

internal partial class TilePyramidDocumentView
{
    private NavigateToAnnotationCommand? _navigateToAnnotationCommand;
    private NavigateToRecognitionCommand? _navigateToRecognitionCommand;

    public ICommand NavigateToRecognitionCommand => _navigateToRecognitionCommand ??=
        LifetimeScope.Resolve<NavigateToRecognitionCommand>(new TypedParameter(typeof(TilePyramidDocumentView), this));

    public ICommand NavigateToAnnotationCommand => _navigateToAnnotationCommand ??=
        LifetimeScope.Resolve<NavigateToAnnotationCommand>(new TypedParameter(typeof(TilePyramidDocumentView), this));

    public ListItemNavigationRequest? RecognitionNavigationRequest { get; private set; }
    public ListItemNavigationRequest? AnnotationNavigationRequest { get; private set; }

    public bool CanNavigateToRecognition(Annotation annotation)
    {
        return CanFocusAnnotation(annotation) && annotation.Source is { } source
                                              && DocumentState.RecognitionBatches.Find(source.BatchId)?.Features
                                                      .Find(source.FeatureId)
                                                  is { Decision: ReviewDecision.Accepted } feature
                                              && feature.AnnotationId == annotation.Id;
    }

    public void NavigateToRecognition(Annotation annotation)
    {
        if (!CanNavigateToRecognition(annotation))
        {
            return;
        }

        // A new association may precede the queued list refresh; resolve a current row before navigating.
        SynchronizeRecognitionList();
        var source = annotation.Source!;
        var row = _recognitionReviewLookup[(source.BatchId, source.FeatureId)];
        ShowRecognitionInList(row);
    }

    private void ShowRecognitionInList(RecognitionReviewItem row)
    {
        SelectedRecognitionBatch = RecognitionBatches.Single(item => ReferenceEquals(item.Batch, row.Batch));
        SetRecognitionListMode(row.Decision);
        DocumentState.FloatingPanels[FloatingPanelIds.Recognition].IsVisible = true;
        RecognitionNavigationRequest = new ListItemNavigationRequest(row);
        OnPropertyChanged(nameof(RecognitionNavigationRequest));
    }

    public bool CanNavigateToAnnotation(RecognitionReviewItem row)
    {
        return CanFocusRecognition(row) && row.Annotation is { Source: { } source }
                                        && source.BatchId == row.BatchId && source.FeatureId == row.Feature.Id;
    }

    public void NavigateToAnnotation(RecognitionReviewItem row)
    {
        if (!CanNavigateToAnnotation(row))
        {
            return;
        }

        ShowAnnotationInList(row.Annotation!);
    }

    private void ShowAnnotationInList(Annotation annotation)
    {
        DocumentState.FloatingPanels[FloatingPanelIds.Annotations].IsVisible = true;
        AnnotationNavigationRequest = new ListItemNavigationRequest(annotation);
        OnPropertyChanged(nameof(AnnotationNavigationRequest));
    }
}
