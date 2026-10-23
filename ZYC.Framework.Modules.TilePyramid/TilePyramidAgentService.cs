using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ZYC.CoreToolkit.Extensions.Autofac.Attributes;
using ZYC.Framework.Modules.TilePyramid.Abstractions;
using ZYC.Framework.Modules.TilePyramid.Abstractions.Agent;
using ZYC.TilePyramid.Annotations;
using ZYC.TilePyramid.Annotations.Wpf;
using ZYC.TilePyramid.Core.Imaging;
using ZYC.TilePyramid.Core.Viewports;

namespace ZYC.Framework.Modules.TilePyramid;

[RegisterSingleInstanceAs(typeof(ITilePyramidAgentService))]
internal partial class TilePyramidAgentService : ITilePyramidAgentService
{
    private readonly TilePyramidConfig _config;
    private readonly TilePyramidAgentDocuments _documents;

    public TilePyramidAgentService(TilePyramidAgentDocuments documents, TilePyramidConfig config,
        TilePyramidAgentTasks agentTasks)
    {
        _documents = documents;
        _config = config;
        AgentTasks = agentTasks;
    }

    private TilePyramidAgentTasks AgentTasks { get; }

    private static HttpClient RegionHttpClient => TilePyramidDocumentTools.SourceHttpClient;

    public Task<TilePyramidAgentDocumentInfo[]> ListDocumentsAsync(CancellationToken cancellationToken = default)
    {
        return OnUIAsync(() => _documents.Items.Select(GetInfo).ToArray(), cancellationToken);
    }

    public Task<TilePyramidAgentDocumentInfo> GetDocumentInfoAsync(Guid documentId,
        CancellationToken cancellationToken = default)
    {
        return OnUIAsync(() => GetInfo(GetTab(documentId)), cancellationToken);
    }

    public Task<TilePyramidAgentViewport> GetViewportAsync(Guid documentId,
        CancellationToken cancellationToken = default)
    {
        return OnUIAsync(() => GetViewport(GetTab(documentId)), cancellationToken);
    }

    public Task<TilePyramidAgentViewport> SetViewportAsync(Guid documentId, Guid taskId,
        TilePyramidNavigationState navigation,
        int? focalPlaneIndex = null, CancellationToken cancellationToken = default)
    {
        // Struct defaults can bypass the navigation state's validating constructor.
        navigation = new TilePyramidNavigationState(navigation.Center, navigation.Scale, navigation.RotationDegrees);
        return OnUIAwaitAsync(async () =>
        {
            var tab = GetTab(documentId);
            AgentTasks.RequireViewportControl(taskId, documentId);
            var task = AgentTasks.RequireRunning(taskId, documentId).Snapshot;
            var plane = focalPlaneIndex ?? task.FocalPlaneIndex;
            if (plane != task.FocalPlaneIndex)
            {
                throw new InvalidOperationException("Viewport navigation must use the task's fixed focal plane.");
            }

            _ = tab.Source.Info.GetFocalPlane(plane);
            if (tab.CreatedView is { } view)
            {
                view.SetAgentViewport(navigation, plane);
            }
            else
            {
                tab.DocumentState.Navigation = navigation;
                tab.DocumentState.FocalPlaneIndex = plane;
                tab.NavigationRequests.OnNext(false);
            }

            await tab.UpdateNavigationUriAsync();
            return GetViewport(GetTab(documentId));
        }, cancellationToken);
    }

    public async Task<TilePyramidAgentRegion> ReadRegionAsync(Guid documentId, ImageRect region,
        int maximumWidth = 1024, int maximumHeight = 1024, int? focalPlaneIndex = null,
        Guid? taskId = null, CancellationToken cancellationToken = default)
    {
        var context = await OnUIAsync(() =>
        {
            var tab = GetTab(documentId);
            var task = taskId is { } id ? AgentTasks.RequireRunning(id, documentId) : null;
            var plane = focalPlaneIndex ?? task?.Snapshot.FocalPlaneIndex ?? GetFocalPlane(tab);
            if (task is not null)
            {
                TilePyramidAgentTasks.RequireRegion(region, task.Snapshot.Region);
                if (plane != task.Snapshot.FocalPlaneIndex)
                {
                    throw new InvalidOperationException("The region must use the task's fixed focal plane.");
                }
            }

            _ = tab.Source.Info.GetFocalPlane(plane);
            return (Document: tab.AgentDocument!, Plane: plane, Lease: tab.AgentDocument!.AcquireSource(),
                TaskToken: task?.Cancellation.Token ?? CancellationToken.None);
        }, cancellationToken).ConfigureAwait(false);

        using var lease = context.Lease;
        using var lifetime =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, lease.CancellationToken,
                context.TaskToken);
        var token = lifetime.Token;
        var bitmap = await AnnotationThumbnailRenderer.RenderRegionAsync(lease.Source, region,
            maximumWidth, maximumHeight, context.Plane, RegionHttpClient, token).ConfigureAwait(false);
        var data = await Task.Run(() =>
        {
            token.ThrowIfCancellationRequested();
            using var output = new MemoryStream();
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            encoder.Save(output);
            token.ThrowIfCancellationRequested();
            return output.ToArray();
        }, token).ConfigureAwait(false);

        var crop = new TilePyramidAgentCrop(Guid.NewGuid(), documentId, region,
            bitmap.PixelWidth, bitmap.PixelHeight, context.Plane);
        await OnUIAsync(() =>
        {
            if (taskId is { } id)
            {
                AgentTasks.RequireRunning(id, documentId);
            }

            var document = GetTab(documentId).AgentDocument!;
            if (!ReferenceEquals(document, context.Document))
            {
                throw new InvalidOperationException("The document was replaced while reading the region.");
            }

            document.Crops.Add(crop.CropId, crop);
            return true;
        }, token).ConfigureAwait(false);
        return new TilePyramidAgentRegion(crop, "image/png", data);
    }

    public Task<Annotation[]> GetAnnotationsAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        return OnUIAsync(() => GetTab(documentId).AnnotationImage.Annotations.Select(CloneAnnotation).ToArray(),
            cancellationToken);
    }

    public Task<TilePyramidAgentRecognitionBatch[]> GetRecognitionBatchesAsync(Guid documentId,
        CancellationToken cancellationToken = default)
    {
        return OnUIAsync(() => GetRecognitionBatches(GetTab(documentId).AnnotationImage), cancellationToken);
    }

    public Task<TilePyramidAgentCandidate[]> GetCandidatesAsync(Guid documentId, Guid batchId,
        CancellationToken cancellationToken = default)
    {
        return OnUIAsync(() => GetTab(documentId).AgentDocument!.RequireBatch(batchId)
                .Candidates.Select(ToCandidate).ToArray(),
            cancellationToken);
    }

    public Task<RecognitionFeature[]> GetRecognitionFeaturesAsync(Guid documentId, Guid batchId,
        CancellationToken cancellationToken = default)
    {
        return OnUIAsync(() => GetTab(documentId).AgentDocument!.RequireBatch(batchId)
            .Features.Select(feature => feature.Clone()).ToArray(), cancellationToken);
    }

    public Task<TilePyramidAgentRecognitionBatch> BeginRecognitionAsync(Guid documentId, string model,
        string? title = null, ImageRect? region = null, int? focalPlaneIndex = null, string layerId = "",
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(model))
        {
            throw new ArgumentException("A model name is required.", nameof(model));
        }

        if ((title is not null && string.IsNullOrWhiteSpace(title)) || layerId is null)
        {
            throw new ArgumentException("Provide a non-blank task title and a non-null target layer ID.");
        }

        return OnUIAsync(() =>
        {
            var tab = GetTab(documentId);
            EnsureDocumentWriteAllowed(tab);
            RequireUnlockedLayer(tab, layerId);
            var bounds = new ImageRect(0, 0, tab.Source.Info.Width, tab.Source.Info.Height);
            var scope = region ?? bounds;
            TilePyramidAgentTasks.RequireRegion(scope, bounds);
            var plane = focalPlaneIndex ?? GetFocalPlane(tab);
            _ = tab.Source.Info.GetFocalPlane(plane);
            var task = AgentTasks.Start(tab, model.Trim(), title?.Trim() ?? "Auto accept", scope, plane, layerId,
                _config.AutoAcceptRecognitionResults);
            return ToBatch(tab.AgentDocument!.RequireBatch(task.BatchId));
        }, cancellationToken);
    }

    public Task<TilePyramidAgentSubmission> SubmitCandidatesAsync(Guid documentId, Guid batchId,
        TilePyramidAgentCandidate[] candidates, Guid? cropId = null, CancellationToken cancellationToken = default)
    {
        if (candidates is null)
        {
            throw new ArgumentNullException(nameof(candidates));
        }

        // Snapshot input before it crosses the dispatcher boundary; never retain caller-owned geometry arrays.
        var features = candidates.Select(CreateFeature).ToArray();
        if (features.Select(f => f.Id).Distinct().Count() != features.Length)
        {
            throw new ArgumentException("Candidate IDs must be unique within a submission.", nameof(candidates));
        }

        return OnUIAsync(() =>
        {
            var tab = GetTab(documentId);
            var document = tab.AgentDocument!;
            var task = AgentTasks.RequireBatch(documentId, batchId).Snapshot;
            var batch = document.RequireBatch(batchId, true);
            if (cropId is { } id)
            {
                if (!document.Crops.TryGetValue(id, out var crop))
                {
                    throw new ArgumentException("Unknown crop ID for this tab. Read the region again.", nameof(cropId));
                }

                if (crop.FocalPlaneIndex != batch.FocalPlaneIndex)
                {
                    throw new InvalidOperationException("The crop's focal plane differs from the recognition batch.");
                }

                foreach (var feature in features)
                {
                    RequireWithin(feature.Shape, crop.PixelWidth, crop.PixelHeight);
                    feature.Shape = feature.Shape.Transform(crop.Bounds.Width / crop.PixelWidth,
                        crop.Bounds.Height / crop.PixelHeight, crop.Bounds.X, crop.Bounds.Y);
                }
            }

            // Complete validation before any collection mutation so a malformed item cannot cause a partial write.
            foreach (var feature in features)
            {
                RequireWithin(feature.Shape, tab.Source.Info.Width, tab.Source.Info.Height);
                TilePyramidAgentTasks.RequireRegion(feature.Bounds, task.Region);
                if (feature.LayerId != task.LayerId)
                {
                    throw new InvalidOperationException("All task results must use the fixed target layer.");
                }
            }

            var pending = features.Where(feature => batch.Features.Find(feature.Id) is null
                or { Decision: ReviewDecision.Pending }).ToArray();
            var skipped = features.Length - pending.Length;
            if (pending.Length == 0)
            {
                return new TilePyramidAgentSubmission(batchId, 0, skipped);
            }

            EnsureDocumentWriteAllowed(tab);
            foreach (var feature in pending)
            {
                RequireUnlockedLayer(tab, feature.LayerId, true);
                if (batch.Features.Find(feature.Id) is { } existing)
                {
                    RequireUnlockedLayer(tab, existing.LayerId, true);
                }
            }

            using var featureUpdate = batch.Features.BeginUpdate();
            using var annotationUpdate = document.Image.Annotations.BeginUpdate();
            if (!document.Workspace.ApplyRecognition(document.Image.ImageId, batch.Id, pending))
            {
                throw new InvalidOperationException("The recognition batch is no longer active.");
            }

            if (task.AutoAcceptRecognitionResults)
            {
                foreach (var feature in pending)
                {
                    tab.DocumentState.AnnotationLayers.GetOrCreate(feature.LayerId);
                    document.Workspace.AcceptRecognition(document.Image.ImageId, batch.Id, feature.Id);
                }
            }

            return new TilePyramidAgentSubmission(batchId, pending.Length, skipped);
        }, cancellationToken);
    }

    public Task EndRecognitionAsync(Guid documentId, Guid batchId, CancellationToken cancellationToken = default)
    {
        return OnUIAsync(() =>
        {
            AgentTasks.Complete(documentId, batchId);
            return true;
        }, cancellationToken);
    }

    public Task<RecognitionFeature> AcceptRecognitionAsync(Guid documentId, Guid batchId, Guid featureId,
        CancellationToken cancellationToken = default)
    {
        return OnUIAsync(() =>
        {
            var (tab, feature) = RequireRecognitionWrite(documentId, batchId, featureId);
            if (feature.Decision == ReviewDecision.Pending)
            {
                tab.DocumentState.AnnotationLayers.GetOrCreate(feature.LayerId);
            }

            var document = tab.AgentDocument!;
            return document.Workspace.AcceptRecognition(document.Image.ImageId, batchId, featureId).Clone();
        }, cancellationToken);
    }

    public Task<RecognitionFeature> RejectRecognitionAsync(Guid documentId, Guid batchId, Guid featureId,
        CancellationToken cancellationToken = default)
    {
        return OnUIAsync(() =>
        {
            var (tab, _) = RequireRecognitionWrite(documentId, batchId, featureId);
            var document = tab.AgentDocument!;
            return document.Workspace.RejectRecognition(document.Image.ImageId, batchId, featureId).Clone();
        }, cancellationToken);
    }

    public Task<RecognitionFeature> ResetRecognitionReviewAsync(Guid documentId, Guid batchId, Guid featureId,
        CancellationToken cancellationToken = default)
    {
        return OnUIAsync(() =>
        {
            var (tab, _) = RequireRecognitionWrite(documentId, batchId, featureId);
            var document = tab.AgentDocument!;
            return document.Workspace.ResetRecognitionReview(document.Image.ImageId, batchId, featureId).Clone();
        }, cancellationToken);
    }

    public Task<RecognitionFeature> CreateRecognitionAnnotationAsync(Guid documentId, Guid batchId, Guid featureId,
        CancellationToken cancellationToken = default)
    {
        return OnUIAsync(() =>
        {
            var (tab, feature) = RequireRecognitionWrite(documentId, batchId, featureId);
            if (feature.Decision == ReviewDecision.Accepted && feature.AnnotationId is null)
            {
                tab.DocumentState.AnnotationLayers.GetOrCreate(feature.LayerId);
            }

            var document = tab.AgentDocument!;
            return document.Workspace.CreateRecognitionAnnotation(document.Image.ImageId, batchId, featureId).Clone();
        }, cancellationToken);
    }

    private (TilePyramidTabItem Tab, RecognitionFeature Feature) RequireRecognitionWrite(
        Guid documentId, Guid batchId, Guid featureId)
    {
        var tab = GetTab(documentId);
        var document = tab.AgentDocument!;
        var feature = document.RequireBatch(batchId).Features.Find(featureId)
                      ?? throw new KeyNotFoundException("The recognition result does not exist in this batch.");
        EnsureDocumentWriteAllowed(tab);
        RequireUnlockedLayer(tab, feature.LayerId, true);
        if (feature.AnnotationId is { } id && document.Image.Annotations.Find(id) is { } annotation)
        {
            RequireUnlockedLayer(tab, annotation.LayerId);
        }

        return (tab, feature);
    }

    private TilePyramidTabItem GetTab(Guid id)
    {
        return _documents.Items.FirstOrDefault(tab => tab.TabReference.Id == id)
               ?? throw new KeyNotFoundException($"Loaded TilePyramid document '{id}' was not found.");
    }

    private static int GetFocalPlane(TilePyramidTabItem tab)
    {
        return tab.DocumentState.FocalPlaneIndex ?? tab.Source.Info.DefaultFocalPlaneIndex;
    }

    private TilePyramidAgentDocumentInfo GetInfo(TilePyramidTabItem tab)
    {
        var info = tab.Source.Info;
        var metadata = tab.Source.Metadata;
        return new TilePyramidAgentDocumentInfo(tab.TabReference.Id, tab.AnnotationImage.ImageId, tab.Title,
            tab.CreateCurrentViewUri().AbsoluteUri,
            new TilePyramidInfo(info.Width, info.Height, info.Levels, info.FocalPlanes, info.DefaultFocalPlaneIndex),
            metadata with { AdditionalProperties = new Dictionary<string, string>(metadata.AdditionalProperties) },
            GetFocalPlane(tab), GetRecognitionBatches(tab.AnnotationImage));
    }

    private TilePyramidAgentRecognitionBatch[] GetRecognitionBatches(AnnotationImage image)
    {
        return image.RecognitionBatches.Select(ToBatch).ToArray();
    }

    private TilePyramidAgentRecognitionBatch ToBatch(RecognitionBatch batch)
    {
        var counts = batch.Features.GroupBy(feature => feature.Decision)
            .ToDictionary(group => group.Key, group => group.Count());
        return new TilePyramidAgentRecognitionBatch(batch.Id, batch.ImageId, batch.Model, batch.FocalPlaneIndex,
            batch.StartedAt, batch.EndedAt, counts.GetValueOrDefault(ReviewDecision.Pending),
            counts.GetValueOrDefault(ReviewDecision.Accepted), counts.GetValueOrDefault(ReviewDecision.Rejected))
        {
            TaskId = AgentTasks.FindTaskId(batch.Id)
        };
    }

    private static TilePyramidAgentViewport GetViewport(TilePyramidTabItem tab)
    {
        return tab.CreatedView?.GetAgentViewport() ?? new TilePyramidAgentViewport(tab.TabReference.Id,
            tab.DocumentState.Navigation, GetFocalPlane(tab), false, false, 0, 0, null, null, null);
    }

    private static Annotation CloneAnnotation(Annotation annotation)
    {
        return annotation.Clone();
    }

    private static TilePyramidAgentCandidate ToCandidate(RecognitionFeature feature)
    {
        return new TilePyramidAgentCandidate
        {
            Id = feature.Id, Label = feature.Label, Score = feature.Score,
            Icon = feature.Icon, Comments = feature.Comments,
            LayerId = feature.LayerId, StrokeArgb = feature.StrokeArgb,
            Shape = feature.Shape switch
            {
                RectangleShape box => new TilePyramidAgentShape
                    { Kind = TilePyramidAgentShapeKind.Rectangle, Region = box.Region },
                EllipseShape ellipse => new TilePyramidAgentShape
                    { Kind = TilePyramidAgentShapeKind.Ellipse, Region = ellipse.Region },
                PolygonShape polygon => new TilePyramidAgentShape
                    { Kind = TilePyramidAgentShapeKind.Polygon, Vertices = polygon.Vertices.ToArray() },
                _ => throw new NotSupportedException("Unknown annotation geometry.")
            }
        };
    }

    private static RecognitionFeature CreateFeature(TilePyramidAgentCandidate candidate)
    {
        // ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (candidate is null || candidate.Id == Guid.Empty || !double.IsFinite(candidate.Score)
            || candidate.Label is null || candidate.Icon is null || candidate.Comments is null
            || candidate.LayerId is null || candidate.Shape is null)
        {
            throw new ArgumentException(
                "A candidate requires a non-empty ID, finite score, geometry and non-null text fields.");
        }

        var geometry = candidate.Shape;
        AnnotationShape shape = geometry.Kind switch
        {
            TilePyramidAgentShapeKind.Rectangle when geometry.Region is { } region && geometry.Vertices is { Length: 0 }
                =>
                new RectangleShape { Region = region },
            TilePyramidAgentShapeKind.Ellipse when geometry.Region is { } region && geometry.Vertices is { Length: 0 }
                =>
                new EllipseShape { Region = region },
            TilePyramidAgentShapeKind.Polygon when geometry.Region is null && geometry.Vertices is { Length: >= 3 } =>
                new PolygonShape { Vertices = geometry.Vertices },
            _ => throw new ArgumentException(
                "Use Region for rectangles/ellipses, or at least three Vertices for polygons.")
        };
        if (!AnnotationGeometry.IsValid(shape))
        {
            throw new ArgumentException("Candidate geometry is invalid.");
        }

        return new RecognitionFeature
        {
            Id = candidate.Id, Label = candidate.Label, Score = candidate.Score,
            Icon = candidate.Icon, Comments = candidate.Comments,
            LayerId = candidate.LayerId, StrokeArgb = candidate.StrokeArgb, Shape = shape
        };
    }

    private static void RequireWithin(AnnotationShape shape, long width, long height)
    {
        if (!AnnotationGeometry.IsWithin(shape, width, height))
        {
            throw new ArgumentException(
                "Candidate geometry must be finite, non-empty and inside the coordinate space.");
        }
    }

    private static Task<T> OnUIAsync<T>(Func<T> action, CancellationToken token)
    {
        return Application.Current.Dispatcher.InvokeAsync(action, DispatcherPriority.Normal, token).Task;
    }

    private static Task<T> OnUIAwaitAsync<T>(Func<Task<T>> action, CancellationToken token)
    {
        return Application.Current.Dispatcher.InvokeAsync(action, DispatcherPriority.Normal, token).Task.Unwrap();
    }
}