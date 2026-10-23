using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using ZYC.CoreToolkit.Extensions.Autofac.Attributes;
using ZYC.Framework.Core.Localizations;
using ZYC.Framework.Modules.TilePyramid.Abstractions.Agent;
using ZYC.TilePyramid.Annotations;
using ZYC.TilePyramid.Core.Viewports;

namespace ZYC.Framework.Modules.TilePyramid;

/// <summary>Session task registry and viewport leases. All access is serialized on the UI dispatcher.</summary>
[RegisterSingleInstance]
internal class TilePyramidAgentTasks : IDisposable
{
    private bool _disposed;
    private bool _expiring;

    public TilePyramidAgentTasks(TilePyramidAgentDocuments documents)
    {
        Documents = documents;
        Timer = new DispatcherTimer(DispatcherPriority.Background, Application.Current.Dispatcher)
        {
            Interval = TimeSpan.FromSeconds(5)
        };
        Timer.Tick += OnTimer;
        Documents.DocumentClosing += OnDocumentClosing;
        Documents.LayerContentsChanged += OnContentsChanged;
    }

    private static TimeSpan LeaseDuration => TimeSpan.FromSeconds(120);
    private Dictionary<Guid, TilePyramidAgentTaskEntry> Entries { get; } = new();
    private TilePyramidAgentDocuments Documents { get; }

    private DispatcherTimer Timer { get; }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Timer.Stop();
        Timer.Tick -= OnTimer;
        Documents.DocumentClosing -= OnDocumentClosing;
        Documents.LayerContentsChanged -= OnContentsChanged;
        foreach (var entry in Entries.Values)
        {
            if (entry.IsActive)
            {
                Transition(entry, TilePyramidAgentTaskState.Interrupted, L.T("The module was unloaded; the external execution state is unknown"));
            }

            entry.Cancellation.Dispose();
        }

        _disposed = true;
    }

    public event EventHandler? Changed;

    public TilePyramidAgentTask Start(TilePyramidTabItem tab, string model, string title, ImageRect region,
        int focalPlane, string layerId, bool autoAcceptRecognitionResults)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var batch = tab.AgentDocument!.BeginRecognition(model, focalPlane);
        var now = DateTimeOffset.UtcNow;
        var snapshot = new TilePyramidAgentTask
        {
            Id = Guid.NewGuid(), DocumentId = tab.TabReference.Id, ImageId = batch.ImageId,
            DocumentTitle = tab.Title, BatchId = batch.Id, Title = title, Model = model, Region = region,
            AutoAcceptRecognitionResults = autoAcceptRecognitionResults,
            FocalPlaneIndex = focalPlane, LayerId = layerId, LayerName = GetLayerName(tab, layerId),
            State = TilePyramidAgentTaskState.Running,
            Stage = L.T("Waiting for the agent to report progress"), StartedAt = now, LastHeartbeatAt = now,
            LeaseExpiresAt = now + LeaseDuration, IsDocumentAvailable = true
        };
        Entries.Add(snapshot.Id, new TilePyramidAgentTaskEntry(snapshot, batch));
        Timer.Start();
        NotifyChanged();
        return snapshot;
    }

    public TilePyramidAgentTask[] GetTasks(Guid? documentId = null)
    {
        ExpireTasks();
        return Entries.Values.Where(entry => documentId is null || entry.Snapshot.DocumentId == documentId)
            .OrderByDescending(entry => entry.Snapshot.StartedAt).Select(Snapshot).ToArray();
    }

    public TilePyramidAgentTask GetTask(Guid taskId)
    {
        return Snapshot(Require(taskId));
    }

    public Guid? FindTaskId(Guid batchId)
    {
        return Entries.Values.FirstOrDefault(entry => entry.Snapshot.BatchId == batchId)?.Snapshot.Id;
    }

    public TilePyramidAgentTaskEntry RequireRunning(Guid taskId, Guid? documentId = null)
    {
        var entry = Require(taskId);
        if (entry.Snapshot.State != TilePyramidAgentTaskState.Running
            || (documentId is { } id && entry.Snapshot.DocumentId != id))
        {
            throw new InvalidOperationException("The task is not running or belongs to another document.");
        }

        return entry;
    }

    public TilePyramidAgentTaskEntry RequireBatch(Guid documentId, Guid batchId)
    {
        var id = FindTaskId(batchId)
                 ?? throw new InvalidOperationException(
                     "This batch has no live session task. Start a new recognition task.");
        return RequireRunning(id, documentId);
    }

    public TilePyramidAgentTask Report(Guid taskId, string? stage, int? completedRegions, int? totalRegions)
    {
        var entry = Require(taskId);
        if (entry.Snapshot.State != TilePyramidAgentTaskState.Running)
        {
            return Snapshot(entry);
        }

        var previous = entry.Snapshot;
        var completed = completedRegions ?? previous.CompletedRegions;
        var total = totalRegions ?? previous.TotalRegions;
        if ((stage is not null && string.IsNullOrWhiteSpace(stage))
            || completed < previous.CompletedRegions || total < 0
            || total < previous.TotalRegions || (total is { } count && completed > count))
        {
            throw new ArgumentException(
                "Progress requires a non-blank stage and non-decreasing counts within the total.");
        }

        var now = DateTimeOffset.UtcNow;
        entry.LeaseTimestamp = Stopwatch.GetTimestamp();
        entry.Snapshot = previous with
        {
            Stage = stage?.Trim() ?? previous.Stage, CompletedRegions = completed, TotalRegions = total,
            LastHeartbeatAt = now, LeaseExpiresAt = now + LeaseDuration
        };
        NotifyChanged();
        return Snapshot(entry);
    }

    public TilePyramidAgentTask RequestCancellation(Guid taskId)
    {
        var entry = Require(taskId);
        if (entry.Snapshot.State == TilePyramidAgentTaskState.Running)
        {
            entry.LeaseTimestamp = Stopwatch.GetTimestamp();
            entry.Snapshot = entry.Snapshot with { LeaseExpiresAt = DateTimeOffset.UtcNow + LeaseDuration };
            Transition(entry, TilePyramidAgentTaskState.CancellationRequested, L.T("Further operations are blocked; waiting for the executor to confirm cancellation"));
        }

        return Snapshot(entry);
    }

    public TilePyramidAgentTask AcknowledgeCancellation(Guid taskId)
    {
        var entry = Require(taskId);
        if (entry.Snapshot.State == TilePyramidAgentTaskState.Cancelled)
        {
            return Snapshot(entry);
        }

        if (entry.Snapshot.State != TilePyramidAgentTaskState.CancellationRequested)
        {
            throw new InvalidOperationException("The task is not awaiting cancellation acknowledgement.");
        }

        Transition(entry, TilePyramidAgentTaskState.Cancelled, L.T("The executor confirmed cancellation; submitted results are retained for review"));
        return Snapshot(entry);
    }

    public void Complete(Guid documentId, Guid batchId)
    {
        var entry = RequireBatch(documentId, batchId);
        Transition(entry, TilePyramidAgentTaskState.Completed, null);
    }

    public TilePyramidAgentTask Fail(Guid taskId, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("A failure explanation is required.", nameof(message));
        }

        var entry = RequireRunning(taskId);
        Transition(entry, TilePyramidAgentTaskState.Failed, message);
        return Snapshot(entry);
    }

    public TilePyramidAgentTask RequestControl(Guid taskId)
    {
        var entry = RequireRunning(taskId);
        if (entry.Snapshot.ControlState == TilePyramidAgentControlState.Revoked)
        {
            throw new InvalidOperationException(
                "The user reclaimed or declined control. Only the UI can grant it again.");
        }

        if (entry.Snapshot.ControlState == TilePyramidAgentControlState.User)
        {
            entry.Snapshot = entry.Snapshot with { ControlState = TilePyramidAgentControlState.Requested };
            NotifyChanged();
        }

        return Snapshot(entry);
    }

    public TilePyramidAgentTask ReleaseControl(Guid taskId)
    {
        var entry = Require(taskId);
        if (entry.Snapshot.ControlState is TilePyramidAgentControlState.Agent or TilePyramidAgentControlState.Requested)
        {
            entry.Snapshot = entry.Snapshot with { ControlState = TilePyramidAgentControlState.User };
            NotifyChanged();
        }

        return Snapshot(entry);
    }

    // Deliberately not exposed through MCP: only a user action can grant viewport control.
    public void GrantControl(Guid taskId)
    {
        var entry = RequireRunning(taskId);
        var tab = Documents.Items.Single(tab => tab.TabReference.Id == entry.Snapshot.DocumentId);
        if (tab.CreatedView?.IsEditingAnnotation == true)
        {
            throw new InvalidOperationException("Finish editing the annotation before granting viewport control.");
        }

        ReclaimControl(entry.Snapshot.DocumentId);
        entry.Snapshot = entry.Snapshot with { ControlState = TilePyramidAgentControlState.Agent };
        NotifyChanged();
    }

    // Called by explicit UI control actions; navigation and editing do not revoke authorization.
    public void ReclaimControl(Guid documentId)
    {
        var changed = false;
        foreach (var entry in Entries.Values.Where(entry => entry.Snapshot.DocumentId == documentId
                                                            && entry.Snapshot.ControlState is
                                                                TilePyramidAgentControlState.Agent
                                                                or TilePyramidAgentControlState.Requested))
        {
            entry.Snapshot = entry.Snapshot with { ControlState = TilePyramidAgentControlState.Revoked };
            changed = true;
        }

        if (changed)
        {
            NotifyChanged();
        }
    }

    public void RequireViewportControl(Guid taskId, Guid documentId)
    {
        if (RequireRunning(taskId, documentId).Snapshot.ControlState != TilePyramidAgentControlState.Agent)
        {
            throw new InvalidOperationException("This task does not have user-granted viewport control.");
        }
    }

    public static void RequireRegion(ImageRect region, ImageRect bounds)
    {
        if (!double.IsFinite(region.X) || !double.IsFinite(region.Y)
                                       || !double.IsFinite(region.Width) || !double.IsFinite(region.Height)
                                       || region.Width <= 0 || region.Height <= 0 || region.Left < bounds.Left ||
                                       region.Top < bounds.Top
                                       || region.Right > bounds.Right || region.Bottom > bounds.Bottom)
        {
            throw new ArgumentException("The region must be finite, non-empty and inside the fixed task scope.");
        }
    }

    private TilePyramidAgentTaskEntry Require(Guid taskId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ExpireTasks();
        return Entries.GetValueOrDefault(taskId) ?? throw new KeyNotFoundException("The session task was not found.");
    }

    private TilePyramidAgentTask Snapshot(TilePyramidAgentTaskEntry entry)
    {
        var tab = Documents.Items.FirstOrDefault(tab => tab.TabReference.Id == entry.Snapshot.DocumentId);
        var counts = entry.Batch.Features.GroupBy(feature => feature.Decision)
            .ToDictionary(group => group.Key, group => group.Count());
        return entry.Snapshot with
        {
            PendingCount = counts.GetValueOrDefault(ReviewDecision.Pending),
            AcceptedCount = counts.GetValueOrDefault(ReviewDecision.Accepted),
            RejectedCount = counts.GetValueOrDefault(ReviewDecision.Rejected),
            IsDocumentAvailable = tab is not null,
            LayerName = tab is null ? entry.Snapshot.LayerName : GetLayerName(tab, entry.Snapshot.LayerId)
        };
    }

    private static string GetLayerName(TilePyramidTabItem tab, string layerId)
    {
        var name = tab.DocumentState.AnnotationLayers.GetName(layerId);
        return string.IsNullOrEmpty(name) ? layerId.Length == 0 ? L.T("Default Layer") : L.T("Deleted target layer") : name;
    }

    private void Transition(TilePyramidAgentTaskEntry entry, TilePyramidAgentTaskState state, string? message)
    {
        entry.Snapshot = entry.Snapshot with
        {
            State = state, ControlState = TilePyramidAgentControlState.User, Message = message,
            EndedAt = state == TilePyramidAgentTaskState.CancellationRequested ? null : DateTimeOffset.UtcNow
        };
        // Close submissions before cancelling pending reads or notifying the UI. No late write can reopen the batch.
        entry.Batch.EndedAt ??= DateTimeOffset.UtcNow;
        entry.Cancellation.Cancel();
        NotifyChanged();
        if (!Entries.Values.Any(item => item.IsActive))
        {
            Timer.Stop();
        }
    }

    private void ExpireTasks()
    {
        if (_expiring)
        {
            return;
        }

        _expiring = true;
        try
        {
            foreach (var entry in Entries.Values.Where(entry => entry.IsActive
                                                                && Stopwatch.GetElapsedTime(entry.LeaseTimestamp) >=
                                                                LeaseDuration).ToArray())
            {
                Transition(entry, TilePyramidAgentTaskState.Interrupted,
                    entry.Snapshot.State == TilePyramidAgentTaskState.CancellationRequested
                        ? L.T("Cancellation was not acknowledged; further operations are blocked and the external execution state is unknown")
                        : L.T("Connection lost; the external execution state is unknown. Canvas control was released and partial results were retained"));
            }
        }
        finally
        {
            _expiring = false;
        }
    }

    private void OnTimer(object? sender, EventArgs e)
    {
        ExpireTasks();
    }

    private void OnContentsChanged(object? sender, EventArgs e)
    {
        NotifyChanged();
    }

    private void OnDocumentClosing(TilePyramidTabItem tab)
    {
        foreach (var entry in Entries.Values.Where(entry => entry.IsActive
                                                            && entry.Snapshot.DocumentId == tab.TabReference.Id)
                     .ToArray())
        {
            Transition(entry, TilePyramidAgentTaskState.Interrupted, L.T("The target tab was closed; further operations are blocked and partial results were retained"));
        }
    }

    private void NotifyChanged()
    {
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
