using ZYC.TilePyramid.Core.Viewports;

namespace ZYC.Framework.Modules.TilePyramid.Abstractions.Agent;

/// <summary>A detached, session-scoped task snapshot. Scope is fixed at creation; results live in BatchId.</summary>
public record TilePyramidAgentTask
{
    /// <summary>Stable task ID for progress, heartbeat and control operations.</summary>
    public Guid Id { get; init; }
    /// <summary>The owning tab ID. Closing this tab interrupts the task.</summary>
    public Guid DocumentId { get; init; }
    /// <summary>The source image identity shared by tabs of the same image.</summary>
    public string ImageId { get; init; } = "";
    /// <summary>The tab title captured at task creation.</summary>
    public string DocumentTitle { get; init; } = "";
    /// <summary>The image-owned batch receiving this task's results.</summary>
    public Guid BatchId { get; init; }
    /// <summary>A human-readable description of the recognition task.</summary>
    public string Title { get; init; } = "";
    /// <summary>The executor-supplied model name.</summary>
    public string Model { get; init; } = "";
    /// <summary>
    ///     Whether this task automatically accepts submitted results through the normal review workflow.
    ///     Captured from the application default at task creation and fixed for the lifetime of the task.
    /// </summary>
    public bool AutoAcceptRecognitionResults { get; init; }
    /// <summary>The immutable recognition bounds in full-resolution image pixels.</summary>
    public ImageRect Region { get; init; }
    /// <summary>The immutable zero-based focal plane.</summary>
    public int FocalPlaneIndex { get; init; }
    /// <summary>The immutable target layer ID; empty means the default layer.</summary>
    public string LayerId { get; init; } = "";
    /// <summary>The target layer's display name, refreshed while the owning tab is available.</summary>
    public string LayerName { get; init; } = "";
    /// <summary>Executor lifecycle, independent of the batch's review decisions.</summary>
    public TilePyramidAgentTaskState State { get; init; }
    /// <summary>The task's current viewport control state.</summary>
    public TilePyramidAgentControlState ControlState { get; init; }
    /// <summary>The executor's current activity, for example reading regions or waiting for a model.</summary>
    public string Stage { get; init; } = "";
    /// <summary>Number of processed regions reported by the executor.</summary>
    public int CompletedRegions { get; init; }
    /// <summary>Total regions when known; null means no percentage can be computed.</summary>
    public int? TotalRegions { get; init; }
    /// <summary>Task creation time in UTC.</summary>
    public DateTimeOffset StartedAt { get; init; }
    /// <summary>Last explicit progress or heartbeat report in UTC. Queries do not renew the lease.</summary>
    public DateTimeOffset LastHeartbeatAt { get; init; }
    /// <summary>Lease deadline, or the fixed cancellation acknowledgement deadline while stopping.</summary>
    public DateTimeOffset LeaseExpiresAt { get; init; }
    /// <summary>Time of a terminal transition; null while running or awaiting cancellation acknowledgement.</summary>
    public DateTimeOffset? EndedAt { get; init; }
    /// <summary>Failure or interruption explanation, when present.</summary>
    public string? Message { get; init; }
    /// <summary>Current number of results awaiting review.</summary>
    public int PendingCount { get; init; }
    /// <summary>Current number of accepted results.</summary>
    public int AcceptedCount { get; init; }
    /// <summary>Current number of rejected results.</summary>
    public int RejectedCount { get; init; }
    /// <summary>Whether the owning tab is still available for result navigation.</summary>
    public bool IsDocumentAvailable { get; init; }
}
