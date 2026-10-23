using System.IO;
using System.Text.Json;
using System.Windows.Media.Imaging;
using ZYC.Framework.Core.Localizations;
using ZYC.Framework.Modules.TilePyramid.Abstractions.Agent;
using ZYC.TilePyramid.Core.Viewports;

namespace ZYC.Framework.Modules.TilePyramid.UI.Debug;

internal partial class AgentDebugSession
{
    private TilePyramidAgentCrop? _crop;

    private Task<TilePyramidAgentRegion> ReadAsync(TilePyramidAgentTask task, ImageRect region, CancellationToken token)
    {
        return Service.ReadRegionAsync(DocumentId, region, focalPlaneIndex: task.FocalPlaneIndex,
            taskId: task.Id, cancellationToken: token);
    }

    private void SetPreview(Guid taskId, TilePyramidAgentRegion region)
    {
        if (_disposed || SelectedTask?.Id != taskId) return;
        using var stream = new MemoryStream(region.Data);
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        RegionImage = image;
        _crop = region.Crop;
        CropText = string.Format(L.T("CropId: {0}\n{1} × {2}; candidates are submitted in crop pixel coordinates."),
            _crop.CropId, _crop.PixelWidth, _crop.PixelHeight);
        Notify(nameof(RegionImage));
        Notify(nameof(CropText));
    }

    private static TilePyramidAgentCandidate CreateCandidate(TilePyramidAgentTask task, Guid id, ImageRect bounds,
        string label, double score, TilePyramidAgentShapeKind kind)
    {
        var region = new ImageRect(bounds.X + bounds.Width * 0.2, bounds.Y + bounds.Height * 0.2,
            bounds.Width * 0.6, bounds.Height * 0.6);
        return new TilePyramidAgentCandidate
        {
            Id = id, Label = label, Score = score, LayerId = task.LayerId,
            Comments = L.T("Local debug simulation placeholder; no recognition model was called."),
            Shape = kind == TilePyramidAgentShapeKind.Polygon
                ? new TilePyramidAgentShape
                {
                    Kind = kind,
                    Vertices = [new ImagePoint(region.Left, region.Top), new ImagePoint(region.Right, region.Top),
                        new ImagePoint(region.Right, region.Bottom), new ImagePoint(region.Left, region.Bottom)]
                }
                : new TilePyramidAgentShape { Kind = kind, Region = region }
        };
    }

    private async Task RefreshResultsAsync(TilePyramidAgentTask task)
    {
        var features = await Service.GetRecognitionFeaturesAsync(DocumentId, task.BatchId, Lifetime.Token);
        if (_disposed || SelectedTask?.Id != task.Id) return;
        var selectedId = SelectedResult?.Id;
        Results.Clear();
        foreach (var feature in features) Results.Add(feature);
        SelectedResult = Results.FirstOrDefault(feature => feature.Id == selectedId);
        ResultJson = JsonSerializer.Serialize(features, JsonOptions);
        Notify(nameof(ResultJson));
    }

    private void SetViewportFields(TilePyramidAgentViewport viewport)
    {
        ViewportJson = JsonSerializer.Serialize(viewport, JsonOptions);
        if (viewport.Navigation is { } navigation)
        {
            CenterX = Format(navigation.Center.X);
            CenterY = Format(navigation.Center.Y);
            Scale = Format(navigation.Scale);
            Rotation = Format(navigation.RotationDegrees);
        }
        Notify();
    }
}
