using ZYC.Framework.Modules.TilePyramid.Abstractions.Agent;

namespace ZYC.Framework.Modules.TilePyramid;

internal partial class TilePyramidAgentService
{
    public Task<TilePyramidAgentTask[]> GetTasksAsync(Guid? documentId = null,
        CancellationToken cancellationToken = default)
    {
        return OnUIAsync(() => AgentTasks.GetTasks(documentId), cancellationToken);
    }

    public Task<TilePyramidAgentTask> GetTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        return OnUIAsync(() => AgentTasks.GetTask(taskId), cancellationToken);
    }


    public Task<TilePyramidAgentTask> ReportTaskProgressAsync(Guid taskId, string? stage = null,
        int? completedRegions = null, int? totalRegions = null, CancellationToken cancellationToken = default)
    {
        return
            OnUIAsync(() => AgentTasks.Report(taskId, stage, completedRegions, totalRegions), cancellationToken);
    }

    public Task<TilePyramidAgentTask> RequestTaskCancellationAsync(Guid taskId,
        CancellationToken cancellationToken = default)
    {
        return OnUIAsync(() => AgentTasks.RequestCancellation(taskId), cancellationToken);
    }


    public Task<TilePyramidAgentTask> AcknowledgeTaskCancellationAsync(Guid taskId,
        CancellationToken cancellationToken = default)
    {
        return OnUIAsync(() => AgentTasks.AcknowledgeCancellation(taskId), cancellationToken);
    }

    public Task<TilePyramidAgentTask> FailTaskAsync(Guid taskId, string message,
        CancellationToken cancellationToken = default)
    {
        return OnUIAsync(() => AgentTasks.Fail(taskId, message), cancellationToken);
    }

    public Task<TilePyramidAgentTask> RequestViewportControlAsync(Guid taskId,
        CancellationToken cancellationToken = default)
    {
        return OnUIAsync(() => AgentTasks.RequestControl(taskId), cancellationToken);
    }

    public Task<TilePyramidAgentTask> ReleaseViewportControlAsync(Guid taskId,
        CancellationToken cancellationToken = default)
    {
        return OnUIAsync(() => AgentTasks.ReleaseControl(taskId), cancellationToken);
    }
}