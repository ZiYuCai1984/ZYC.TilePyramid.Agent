namespace ZYC.Framework.Modules.TilePyramid.Abstractions.Agent;

public partial interface ITilePyramidAgentService
{
    /// <summary>Lists this application session's task snapshots, optionally restricted to an owning tab. Does not renew leases.</summary>
    Task<TilePyramidAgentTask[]> GetTasksAsync(Guid? documentId = null, CancellationToken cancellationToken = default);

    /// <summary>Gets task state, fixed scope, control ownership and result counts. Does not renew the lease.</summary>
    Task<TilePyramidAgentTask> GetTaskAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Reports progress and renews a running task's 120-second lease. Call at least every 30 seconds, including during
    ///     long model calls. Null arguments retain prior progress. Counts cannot decrease or exceed a known total.
    ///     For a stopping or terminal task, returns its state without renewing the lease; stop work when State is not Running.
    /// </summary>
    Task<TilePyramidAgentTask> ReportTaskProgressAsync(Guid taskId, string? stage = null,
        int? completedRegions = null, int? totalRegions = null, CancellationToken cancellationToken = default);

    /// <summary>Blocks further task reads, submissions and viewport changes immediately, preserving partial results. Await executor acknowledgement.</summary>
    Task<TilePyramidAgentTask> RequestTaskCancellationAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>Confirms that the executor stopped after a cancellation request. Repeating acknowledgement is a no-op.</summary>
    Task<TilePyramidAgentTask> AcknowledgeTaskCancellationAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>Reports a running task's failure, closes its batch and retains partial results.</summary>
    Task<TilePyramidAgentTask> FailTaskAsync(Guid taskId, string message, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Requests viewport control from the user. Does not grant control. Poll task state or report heartbeats while
    ///     waiting. A user-revoked request cannot be renewed by the agent; recognition may continue in the background.
    /// </summary>
    Task<TilePyramidAgentTask> RequestViewportControlAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>Voluntarily returns viewport control to the user without stopping recognition.</summary>
    Task<TilePyramidAgentTask> ReleaseViewportControlAsync(Guid taskId, CancellationToken cancellationToken = default);
}
