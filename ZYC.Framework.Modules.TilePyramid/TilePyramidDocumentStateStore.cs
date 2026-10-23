using System.IO;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ZYC.CoreToolkit.Extensions.Autofac.Attributes;
using ZYC.Framework.Abstractions;
using ZYC.Framework.Modules.TilePyramid.Abstractions;
using ZYC.TilePyramid.Annotations;
using ZYC.TilePyramid.Core.Viewports;

namespace ZYC.Framework.Modules.TilePyramid;

[RegisterSingleInstance]
internal partial class TilePyramidDocumentStateStore : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly Dictionary<string, TilePyramidDocumentState> _pendingLocalSnapshots =
        new(StringComparer.OrdinalIgnoreCase);

    // Runtime states keep view preferences and stable annotation/batch collections shared by all tabs.
    private readonly Dictionary<string, TilePyramidDocumentState> _runtimeStates =
        new(StringComparer.Ordinal);

    private readonly Subject<bool> _saveRequests = new();


    public TilePyramidDocumentStateStore(
        TilePyramidState state,
        IAppContext appContext,
        ILogger<TilePyramidDocumentStateStore> logger)
    {
        State = state;
        AppContext = appContext;
        Logger = logger;

        AppContext.Exit += OnAppExit;

        _saveRequests
            .Throttle(TimeSpan.FromMilliseconds(512))
            .Where(saveRequested => saveRequested)
            .ObserveOn(AppContext.GetUISynchronizationContext())
            .Subscribe(_ => Flush())
            .DisposeWith(CompositeDisposable);
        _saveRequests.DisposeWith(CompositeDisposable);
    }


    private TilePyramidState State { get; }

    private IAppContext AppContext { get; }

    private ILogger<TilePyramidDocumentStateStore> Logger { get; }

    public async Task<TilePyramidDocumentState> LoadAsync(TilePyramidDocumentRoute route)
    {
        var key = GetRuntimeKey(route);
        if (_runtimeStates.TryGetValue(key, out var current))
        {
            return current.CreateTabState();
        }

        if (route.LocalStatePath is not { } path)
        {
            var document = State.Documents.TryGetValue(route.StateKey, out var state)
                ? Validate(state).Clone()
                : new TilePyramidDocumentState();
            _runtimeStates[key] = document;

            return document.CreateTabState();
        }

        try
        {
            await using var stream = File.OpenRead(path);
            var state = await JsonSerializer.DeserializeAsync<TilePyramidDocumentState>(stream, JsonOptions);
            // A tab may have updated the same document while the file was being read.
            if (!_runtimeStates.TryGetValue(key, out current))
            {
                current = Validate(state);
                _runtimeStates[key] = current;
            }

            return current.CreateTabState();
        }
        catch (Exception e) when (e is FileNotFoundException or DirectoryNotFoundException)
        {
            if (!_runtimeStates.TryGetValue(key, out current))
            {
                current = new TilePyramidDocumentState();
                _runtimeStates[key] = current;
            }

            return current.CreateTabState();
        }
        catch (Exception e)
        {
            Logger.Error(e);
            // Do not replace unreadable saved annotations with an empty document.
            throw;
        }
    }

    public void Save(TilePyramidDocumentRoute route, TilePyramidDocumentState state)
    {
        var document = Validate(state).CreateTabState();
        if (GetLatest(route) is { } latest)
        {
            document.Annotations = latest.Annotations;
            document.RecognitionBatches = latest.RecognitionBatches;
            document.AnnotationLayers = latest.AnnotationLayers;
        }

        SaveDocument(route, document);
    }

    public void SaveAnnotationWorkspace(TilePyramidDocumentRoute route, TilePyramidDocumentState state)
    {
        Validate(state);
        var document = (GetLatest(route) ?? state).CreateTabState();
        // Annotations and recognition results contain paired references and must be saved together.
        document.Annotations = state.Annotations;
        document.RecognitionBatches = state.RecognitionBatches;
        SaveDocument(route, document);
    }

    private TilePyramidDocumentState? GetLatest(TilePyramidDocumentRoute route)
    {
        return _runtimeStates.GetValueOrDefault(GetRuntimeKey(route));
    }

    public void SaveAnnotationLayers(TilePyramidDocumentRoute route, TilePyramidDocumentState state)
    {
        var document = (GetLatest(route) ?? state).CreateTabState();
        document.AnnotationLayers = state.AnnotationLayers;
        SaveDocument(route, document);
    }

    private static string GetRuntimeKey(TilePyramidDocumentRoute route)
    {
        // File paths ignore case; document URIs retain case-sensitive source paths.
        return route.LocalStatePath?.ToUpperInvariant() ?? route.StateKey;
    }

    private void SaveDocument(TilePyramidDocumentRoute route, TilePyramidDocumentState document)
    {
        // Only persistence receives deep copies; saving must not replace the tabs' live collection.
        var snapshot = document.Clone();
        _runtimeStates[GetRuntimeKey(route)] = document;
        if (route.LocalStatePath is { } path)
        {
            // View and annotation changes are merged independently. Closing a tab only flushes.
            _pendingLocalSnapshots[path] = snapshot;
            _saveRequests.OnNext(true);
        }
        else
        {
            // Framework owns persistence of this IState. Replace the dictionary so readers
            // never enumerate a dictionary while this method mutates it.
            State.Documents = new Dictionary<string, TilePyramidDocumentState>(State.Documents)
            {
                [route.StateKey] = snapshot
            };
        }
    }

    public void Flush()
    {
        // Cancel the pending debounced request before saving immediately.
        if (!_disposed)
        {
            _saveRequests.OnNext(false);
        }

        foreach (var (path, snapshot) in _pendingLocalSnapshots.ToArray())
        {
            try
            {
                WriteLocalState(path, snapshot);
                _pendingLocalSnapshots.Remove(path);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                // Retain the pending snapshot for the next save/close/exit attempt.
                Logger.Error(new IOException($"Could not save TilePyramid document state to '{path}'.", e));
            }
        }
    }

    private void OnAppExit(object? sender, EventArgs e)
    {
        Flush();
    }

    private static TilePyramidDocumentState Validate(TilePyramidDocumentState? state)
    {
        if (state is null)
        {
            throw new InvalidDataException("The TilePyramid document state must be a JSON object.");
        }

        if (state.Navigation is { } navigation)
        {
            _ = new TilePyramidNavigationState(navigation.Center, navigation.Scale, navigation.RotationDegrees);
        }

        if (state.FocalPlaneIndex is < 0)
        {
            throw new InvalidDataException("The focal-plane index must be non-negative.");
        }

        if (state.FloatingPanels is null)
        {
            throw new InvalidDataException("The floating panel layouts must be a JSON object.");
        }

        foreach (var panel in state.FloatingPanels.Values)
        {
            if (panel is null || !Enum.IsDefined(panel.Position)
                              || (panel.Width is { } width && (!double.IsFinite(width) || width <= 0))
                              || (panel.Height is { } height && (!double.IsFinite(height) || height <= 0)))
            {
                throw new InvalidDataException(
                    "Floating panels must have a valid anchor and positive, finite preferred dimensions.");
            }
        }

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (state.Annotations is null || state.Annotations.Any(item =>
                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                item.Shape is null || !AnnotationGeometry.IsValid(item.Shape)))
        {
            throw new InvalidDataException("The TilePyramid document contains invalid annotations.");
        }

        if (state.RecognitionBatches is null || state.RecognitionBatches.Any(batch =>
                batch.Id == Guid.Empty || string.IsNullOrWhiteSpace(batch.ImageId)
                                       || string.IsNullOrWhiteSpace(batch.Model) || batch.FocalPlaneIndex < 0
                                       || batch.Features is null || batch.Features.Any(feature =>
                                           feature.Id == Guid.Empty || !double.IsFinite(feature.Score) ||
                                           !Enum.IsDefined(feature.Decision)
                                           || feature.Shape is null || !AnnotationGeometry.IsValid(feature.Shape)
                                           || feature.Label is null || feature.LayerId is null
                                           || feature.Icon is null || feature.Comments is null)))
        {
            throw new InvalidDataException("The TilePyramid document contains invalid recognition batches.");
        }

        ValidateRecognitionAssociations(state);
        return state;
    }

    private static void ValidateRecognitionAssociations(TilePyramidDocumentState state)
    {
        foreach (var batch in state.RecognitionBatches)
        {
            foreach (var feature in batch.Features)
            {
                if (feature.Decision != ReviewDecision.Accepted && feature.AnnotationId is not null)
                {
                    throw new InvalidDataException(
                        "The recognition decision does not match its annotation association.");
                }

                if (feature.AnnotationId is { } id)
                {
                    var annotation = state.Annotations.Find(id);
                    if (annotation?.Source is not { } source
                        || source.BatchId != batch.Id || source.FeatureId != feature.Id)
                    {
                        throw new InvalidDataException(
                            "The associated annotation does not reference its recognition result.");
                    }
                }
            }
        }

        foreach (var annotation in state.Annotations)
        {
            if (annotation.Source is { } source
                && (state.RecognitionBatches.Find(source.BatchId)?.Features.Find(source.FeatureId) is not { } feature
                    || feature.AnnotationId != annotation.Id || feature.Decision != ReviewDecision.Accepted))
            {
                throw new InvalidDataException(
                    "The annotation's recognition source does not reference this annotation.");
            }
        }
    }

    private static void WriteLocalState(string path, TilePyramidDocumentState state)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporaryPath = path + $".{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(state, JsonOptions));
            File.Move(temporaryPath, path, true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }
}
