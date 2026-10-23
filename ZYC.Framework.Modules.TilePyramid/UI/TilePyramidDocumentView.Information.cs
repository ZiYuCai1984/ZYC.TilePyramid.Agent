using System.Net.Http;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Autofac;
using Microsoft.Extensions.Logging;
using ZYC.Framework.Core.Localizations;
using ZYC.Framework.Modules.TilePyramid.Commands;
using ZYC.TilePyramid.Core.Imaging;
using ZYC.TilePyramid.Core.Sources;
using ZYC.TilePyramid.Wpf;

namespace ZYC.Framework.Modules.TilePyramid.UI;

internal partial class TilePyramidDocumentView
{
    private readonly CancellationTokenSource _labelImageCancellation = new();
    private ICommand? _rotateLabelImageCommand;
    private static HttpClient LabelImageHttpClient => TilePyramidDocumentTools.SourceHttpClient;

    public ICommand RotateLabelImageCommand => _rotateLabelImageCommand ??=
        LifetimeScope.Resolve<RotateLabelImageCommand>(
            new TypedParameter(typeof(TilePyramidDocumentView), this));

    public IReadOnlyList<KeyValuePair<string, string>> DocumentInformation { get; private set; } = [];

    public IReadOnlyList<KeyValuePair<string, string>> DocumentTechnicalInformation { get; private set; } = [];

    public BitmapSource? LabelImage { get; private set; }

    public bool HasLabelImage => LabelImage is not null;

    public string LabelImageTitle { get; private set; } = L.T("Label Image");

    public string LabelImageStatus { get; private set; } = L.T("No label or macro image");

    private void InitializeDocumentInformation()
    {
        var metadata = Source.Metadata;
        var info = Source.Info;
        var fullResolutionLevel = info.Levels.MaxBy(level => level.Width)!;
        var information = new List<KeyValuePair<string, string>>();
        AddInformation(information, L.T("Name"), metadata.Name ?? TabItem.Title);
        AddInformation(information, L.T("Format"), metadata.SourceFormat);
        AddInformation(information, L.T("Vendor"), metadata.Vendor);
        AddInformation(information, L.T("Level Count"), info.Levels.Count.ToString());
        AddInformation(information, L.T("Focal Plane Count"), info.FocalPlanes.Count.ToString());
        AddInformation(information, L.T("Image Size"), $"{info.Width:N0} × {info.Height:N0} px");

        var magnification = metadata.AdditionalProperties.GetValueOrDefault("openslide.objective-power")
                            ?? metadata.AdditionalProperties.GetValueOrDefault("aperio.AppMag");
        if (!string.IsNullOrWhiteSpace(magnification))
        {
            AddInformation(information, L.T("Scan Magnification"), $"{magnification}×");
        }

        if (metadata.PixelSize is { } pixelSize)
        {
            var horizontal = pixelSize.HorizontalMicrometersPerPixel;
            var vertical = pixelSize.VerticalMicrometersPerPixel;
            if (horizontal.Equals(vertical))
            {
                AddInformation(information, L.T("Resolution"), $"{horizontal:G6} μm/px");
            }
            else
            {
                AddInformation(information, L.T("Horizontal Resolution"), $"{horizontal:G6} μm/px");
                AddInformation(information, L.T("Vertical Resolution"), $"{vertical:G6} μm/px");
            }
        }

        AddInformation(information, L.T("Notes"), metadata.Comments);
        DocumentInformation = information;

        var technicalInformation = new List<KeyValuePair<string, string>>
        {
            new(L.T("Horizontal Tile Count"), fullResolutionLevel.ColumnCount.ToString("N0")),
            new(L.T("Vertical Tile Count"), fullResolutionLevel.RowCount.ToString("N0")),
            new(L.T("Tile Size"), $"{fullResolutionLevel.TileWidth} × {fullResolutionLevel.TileHeight} px"),
            new(L.T("Minimum Level Index"), "0"),
            new(L.T("Maximum Level Index"), (info.Levels.Count - 1).ToString())
        };
        AddInformation(technicalInformation, L.T("Quick Hash"), metadata.QuickHash);
        foreach (var property in metadata.AdditionalProperties)
        {
            AddInformation(technicalInformation, property.Key, property.Value);
        }

        DocumentTechnicalInformation = technicalInformation;
    }

    private static void AddInformation(List<KeyValuePair<string, string>> items, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            items.Add(new KeyValuePair<string, string>(name, value));
        }
    }

    private async Task LoadLabelImageAsync()
    {
        if (Source is not IAssociatedImageSource associatedSource)
        {
            return;
        }

        // A macro can include the printed label, so use it when no label image
        // is available and identify it as a macro in the UI title.
        var preview = associatedSource.AssociatedImages.FirstOrDefault(image => image.Name == "label")
                      ?? associatedSource.AssociatedImages.FirstOrDefault(image => image.Name == "macro");
        if (preview is null)
        {
            return;
        }

        LabelImageTitle = preview.Name switch
        {
            "macro" => L.T("Macro Image"),
            _ => L.T("Label Image")
        };
        LabelImageStatus = L.T("Loading associated image…");
        OnPropertyChanged(nameof(LabelImageTitle));
        OnPropertyChanged(nameof(LabelImageStatus));
        try
        {
            // Keep the source alive until the image read completes, including when the tab closes.
            using var lease = TabItem.AgentDocument!.AcquireSource();
            using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(
                lease.CancellationToken, _labelImageCancellation.Token);
            var token = cancellation.Token;
            var bitmap = await Task.Run(async () =>
            {
                var resource = await associatedSource.ResolveAssociatedImageAsync(preview.Name, token);
                BitmapSource image;
                if (resource is TileResource.PixelBuffer pixels)
                {
                    var format = pixels.PixelFormat switch
                    {
                        TilePixelFormat.Bgra32 => PixelFormats.Bgra32,
                        _ => throw new NotSupportedException(
                            $"Unsupported associated image pixel format: {pixels.PixelFormat}")
                    };
                    image = BitmapSource.Create(pixels.Width, pixels.Height, 96, 96, format, null,
                        pixels.Pixels.ToArray(), pixels.Stride);
                    image.Freeze();
                }
                else
                {
                    var content = await TileResourceReader.ReadAsync(resource, LabelImageHttpClient, token);
                    image = await TilePyramidView.DecodeBitmapAsync(content, token);
                }

                token.ThrowIfCancellationRequested();
                return image;
            }, token);

            if (_disposed)
            {
                return;
            }

            LabelImage = bitmap;
            LabelImageStatus = "";
            OnPropertyChanged(nameof(LabelImage));
            OnPropertyChanged(nameof(HasLabelImage));
            OnPropertyChanged(nameof(LabelImageStatus));
            (_rotateLabelImageCommand as RotateLabelImageCommand)?.RaiseCanExecuteChanged();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to load the slide associated image '{ImageName}'.", preview.Name);
            if (!_disposed)
            {
                LabelImageStatus = L.T("Failed to load associated image");
                OnPropertyChanged(nameof(LabelImageStatus));
            }
        }
    }
}