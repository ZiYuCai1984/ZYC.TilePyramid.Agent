namespace ZYC.Framework.Modules.TilePyramid.Abstractions.Agent;

/// <summary>The execution state of an external recognition task, independent of result review.</summary>
public enum TilePyramidAgentTaskState
{
    /// <summary>The executor may read task regions and submit results.</summary>
    Running,
    /// <summary>Local operations are blocked; the executor has not yet acknowledged cancellation.</summary>
    CancellationRequested,
    /// <summary>The executor reported successful completion. Results may still require review.</summary>
    Completed,
    /// <summary>The executor acknowledged cancellation.</summary>
    Cancelled,
    /// <summary>The executor reported a failure. Partial results are retained.</summary>
    Failed,
    /// <summary>The lease expired or the document closed. The external executor's state is unknown.</summary>
    Interrupted
}
