using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Reactive.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Media.Imaging;
using ZYC.Framework.Abstractions;
using ZYC.Framework.Core;
using ZYC.Framework.Core.Localizations;
using ZYC.Framework.Modules.TilePyramid.Abstractions.Agent;
using ZYC.Framework.Modules.TilePyramid.UI.AgentTasks;
using ZYC.TilePyramid.Annotations;

namespace ZYC.Framework.Modules.TilePyramid.UI.Debug;

// Owned by the document view: hiding the panel must not stop an executor or its heartbeat.
internal partial class AgentDebugSession : INotifyPropertyChanged, IDisposable
{
    private bool _initialized;
    private bool _disposed;
    private bool _refreshing;
    private bool _busy;
    private bool _maintaining;
    private IDisposable? _clock;
    private Guid? _operationTaskId;
    private AgentTaskItem? _selectedTask;
    private RecognitionFeature? _selectedResult;

    public AgentDebugSession(Guid documentId, TilePyramidAgentTasks tasks, ITilePyramidAgentService service,
        Action<Guid> showResults)
    {
        DocumentId = documentId;
        Registry = tasks;
        Service = service;
        ShowResults = showResults;
    }

    private Guid DocumentId { get; }
    private TilePyramidAgentTasks Registry { get; }
    private ITilePyramidAgentService Service { get; }
    private Action<Guid> ShowResults { get; }
    private CancellationTokenSource Lifetime { get; } = new();
    private HashSet<Guid> OwnedTaskIds { get; } = [];
    private Queue<string> LogEntries { get; } = new();
    private static JsonSerializerOptions JsonOptions { get; } = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public ObservableCollection<AgentTaskItem> Tasks { get; } = [];
    public ObservableCollection<TilePyramidAgentLayer> Layers { get; } = [];
    public ObservableCollection<RecognitionFeature> Results { get; } = [];
    public TilePyramidAgentLayer? SelectedLayer { get; set; }
    public string Title { get; set; } = L.T("Local debug recognition");
    public string Model { get; set; } = "LocalDebug";
    public string RegionX { get; set; } = "0";
    public string RegionY { get; set; } = "0";
    public string RegionWidth { get; set; } = "256";
    public string RegionHeight { get; set; } = "256";
    public string FocalPlane { get; set; } = "1";
    public string LayerName { get; set; } = L.T("Local debug");
    public string CandidateId { get; set; } = Guid.NewGuid().ToString();
    public string CandidateLabel { get; set; } = L.T("Debug placeholder candidate");
    public string CandidateScore { get; set; } = "0.9";
    public TilePyramidAgentShapeKind CandidateShape { get; set; }
    public TilePyramidAgentShapeKind[] ShapeKinds { get; } = Enum.GetValues<TilePyramidAgentShapeKind>();
    public string Stage { get; set; } = L.T("Local debug");
    public string CompletedRegions { get; set; } = "0";
    public string TotalRegions { get; set; } = "";
    public string FailureMessage { get; set; } = L.T("Local simulation failed");
    public string SimulationRegions { get; set; } = "16";
    public string SimulationDelay { get; set; } = "1500";
    public bool AutomaticHeartbeat { get; set; } = true;
    public bool AutomaticAcknowledgement { get; set; }
    public bool MoveViewportDuringSimulation { get; set; }
    public string CenterX { get; set; } = "0";
    public string CenterY { get; set; } = "0";
    public string Scale { get; set; } = "1";
    public string Rotation { get; set; } = "0";
    public BitmapImage? RegionImage { get; private set; }
    public string CropText { get; private set; } = L.T("No region has been read. Candidates will use the task scope until a region is read.");
    public string ViewportJson { get; private set; } = "";
    public string ResultJson { get; private set; } = "";
    public string Log => string.Join(Environment.NewLine, LogEntries);
    public string Activity => _busy ? L.T("API call in progress; stopping the task and reclaiming control remain available") : L.T("Ready");
    public string SimulationStatus => _simulationTaskId is { } id ? string.Format(L.T("Simulation running: {0}"), id) : L.T("Simulator idle");
    public string TaskJson => SelectedTask is { } item ? JsonSerializer.Serialize(item.Snapshot, JsonOptions) : L.T("No task selected");
    public string LeaseText => SelectedTask is { Snapshot: var task }
        ? string.Format(L.T("{0} / {1} · Lease remaining {2:F0}s"), task.State, task.ControlState,
            Math.Max(0, (task.LeaseExpiresAt - DateTimeOffset.UtcNow).TotalSeconds))
          + (OwnedTaskIds.Contains(task.Id) ? L.T(" · Local executor") : L.T(" · External executor (no automatic lease renewal)"))
        : L.T("No tasks in the current document");

    public AgentTaskItem? SelectedTask
    {
        get => _selectedTask;
        set
        {
            if (_selectedTask == value) return;
            _selectedTask = value;
            _crop = null;
            RegionImage = null;
            CropText = L.T("No region has been read. Candidates will use the task scope until a region is read.");
            CandidateId = Guid.NewGuid().ToString();
            Results.Clear();
            SelectedResult = null;
            ResultJson = "";
            if (value is not null)
            {
                SetProgressFields(value.Snapshot);
            }
            Notify();
        }
    }

    public RecognitionFeature? SelectedResult
    {
        get => _selectedResult;
        set
        {
            _selectedResult = value;
            if (value is not null)
            {
                CandidateId = value.Id.ToString();
                Notify(nameof(CandidateId));
            }
            Notify(nameof(SelectedResult));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public async Task InitializeAsync()
    {
        if (_initialized || _disposed) return;
        _initialized = true;
        Registry.Changed += OnTasksChanged;
        _clock = Observable.Interval(TimeSpan.FromSeconds(1)).ObserveOnUI().Subscribe(tick => { _ = MaintainAsync(); });
        await ExecuteAsync("Initialize");
    }

    private void OnTasksChanged(object? sender, EventArgs e)
    {
        RefreshTasks();
    }

    private void RefreshTasks()
    {
        if (_disposed || _refreshing) return;
        _refreshing = true;
        try
        {
            foreach (var snapshot in Registry.GetTasks(DocumentId).Reverse())
            {
                var item = Tasks.FirstOrDefault(item => item.Id == snapshot.Id);
                if (item is null) Tasks.Insert(0, new AgentTaskItem(snapshot));
                else item.Update(snapshot);
            }
            SelectedTask ??= Tasks.FirstOrDefault();
            if (_simulationTaskId is { } id && Registry.GetTask(id).State != TilePyramidAgentTaskState.Running)
                _simulationCancellation?.Cancel();
            Notify(nameof(TaskJson));
            Notify(nameof(LeaseText));
        }
        finally
        {
            _refreshing = false;
        }
    }

    private async Task MaintainAsync()
    {
        if (_disposed || _maintaining) return;
        _maintaining = true;
        try
        {
            RefreshTasks();
            foreach (var id in OwnedTaskIds.ToArray())
            {
                if (_disposed) break;
                var task = Registry.GetTask(id);
                if (AutomaticHeartbeat && task.State == TilePyramidAgentTaskState.Running
                    && DateTimeOffset.UtcNow - task.LastHeartbeatAt >= TimeSpan.FromSeconds(15))
                    await Service.ReportTaskProgressAsync(id, cancellationToken: Lifetime.Token);
                if (AutomaticAcknowledgement && task.State == TilePyramidAgentTaskState.CancellationRequested
                    && _simulationTaskId != id && _operationTaskId != id)
                {
                    await Service.AcknowledgeTaskCancellationAsync(id, Lifetime.Token);
                    WriteLog(string.Format(L.T("Cancellation acknowledged automatically: {0}"), id));
                }
            }
        }
        catch (OperationCanceledException) when (_disposed) { }
        catch (Exception exception) { WriteLog(string.Format(L.T("Heartbeat or cancellation acknowledgement failed: {0}"), exception)); }
        finally { _maintaining = false; }
    }

    private TilePyramidAgentTask RequireTask()
    {
        return SelectedTask is { } item ? Registry.GetTask(item.Id) : throw new InvalidOperationException(L.T("Create or select a task first."));
    }

    private void SetProgressFields(TilePyramidAgentTask task)
    {
        Stage = task.Stage;
        CompletedRegions = task.CompletedRegions.ToString(CultureInfo.InvariantCulture);
        TotalRegions = task.TotalRegions?.ToString(CultureInfo.InvariantCulture) ?? "";
    }

    private static double Number(string text, string field)
    {
        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || !double.IsFinite(value))
            throw new ArgumentException(string.Format(L.T("{0} must be a finite number. Use '.' as the decimal separator."), field));
        return value;
    }

    private static int Integer(string text, string field, int minimum = 0, int maximum = int.MaxValue)
    {
        if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            || value < minimum || value > maximum)
            throw new ArgumentException(string.Format(L.T("{0} must be an integer between {1} and {2}."), field, minimum, maximum));
        return value;
    }

    private static string Format(double value) => value.ToString("G17", CultureInfo.InvariantCulture);

    private void WriteLog(string message)
    {
        if (_disposed) return;
        LogEntries.Enqueue($"[{DateTime.Now:HH:mm:ss}] {message}");
        while (LogEntries.Count > 200) LogEntries.Dequeue();
        Notify(nameof(Log));
    }

    private void Notify(string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Registry.Changed -= OnTasksChanged;
        _clock?.Dispose();
        _simulationCancellation?.Cancel();
        Lifetime.Cancel();
        Lifetime.Dispose();
        RegionImage = null;
    }
}
