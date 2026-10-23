using ZYC.Framework.Core.Localizations;
using ZYC.Framework.Modules.TilePyramid.Abstractions.Agent;
using ZYC.TilePyramid.Core.Viewports;

namespace ZYC.Framework.Modules.TilePyramid.UI.Debug;

internal partial class AgentDebugSession
{
    private Guid? _simulationTaskId;
    private CancellationTokenSource? _simulationCancellation;

    private void StartSimulation(TilePyramidAgentTask task, int count, int delay, string label, double score,
        TilePyramidAgentShapeKind shape)
    {
        var cancellation = CancellationTokenSource.CreateLinkedTokenSource(Lifetime.Token);
        _simulationTaskId = task.Id;
        _simulationCancellation = cancellation;
        Notify(nameof(SimulationStatus));
        _ = RunSimulationAsync(task, count, delay, label, score, shape, cancellation);
    }

    private async Task RunSimulationAsync(TilePyramidAgentTask task, int count, int delay, string label, double score,
        TilePyramidAgentShapeKind shape, CancellationTokenSource cancellation)
    {
        var token = cancellation.Token;
        try
        {
            WriteLog(string.Format(L.T("Automatic execution started: {0}; {1} regions, placeholder candidates only."), task.Id, count));
            for (var index = 0; index < count; index++)
            {
                token.ThrowIfCancellationRequested();
                var progress = await Service.ReportTaskProgressAsync(task.Id, string.Format(L.T("Reading region {0}/{1}"), index + 1, count),
                    index, count, token);
                if (progress.State != TilePyramidAgentTaskState.Running) return;
                var bounds = GetSimulationRegion(task.Region, index, count);
                await MoveSimulationViewportAsync(task, bounds, token);
                var region = await ReadAsync(task, bounds, token);
                SetPreview(task.Id, region);
                if (delay > 0) await Task.Delay(delay, token);
                token.ThrowIfCancellationRequested();
                var candidate = CreateCandidate(task, Guid.NewGuid(),
                    new ImageRect(0, 0, region.Crop.PixelWidth, region.Crop.PixelHeight),
                    $"{label} {index + 1}", score, shape);
                await Service.SubmitCandidatesAsync(DocumentId, task.BatchId, [candidate], region.Crop.CropId, token);
                if (SelectedTask?.Id == task.Id)
                {
                    CandidateId = candidate.Id.ToString();
                    Notify(nameof(CandidateId));
                }
                await Service.ReportTaskProgressAsync(task.Id, string.Format(L.T("Submitted region {0}/{1}"), index + 1, count), index + 1, count, token);
                WriteLog(string.Format(L.T("Simulation submission {0}/{1}: {2}"), index + 1, count, candidate.Id));
            }
            token.ThrowIfCancellationRequested();
            await Service.EndRecognitionAsync(DocumentId, task.BatchId, token);
            WriteLog(string.Format(L.T("Automatic execution completed: {0}"), task.Id));
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            WriteLog(string.Format(L.T("Simulator stopped: {0}"), task.Id));
        }
        catch (Exception exception)
        {
            WriteLog(string.Format(L.T("Automatic execution exited: {0}"), exception));
            if (!_disposed)
            {
                try
                {
                    if (Registry.GetTask(task.Id).State == TilePyramidAgentTaskState.Running)
                        await Service.FailTaskAsync(task.Id, exception.Message, Lifetime.Token);
                }
                catch (Exception failure) { WriteLog(string.Format(L.T("Could not report the failure state: {0}"), failure)); }
            }
        }
        finally
        {
            _simulationTaskId = null;
            _simulationCancellation = null;
            cancellation.Dispose();
            if (!_disposed)
            {
                try
                {
                    if (AutomaticAcknowledgement
                        && Registry.GetTask(task.Id).State == TilePyramidAgentTaskState.CancellationRequested)
                    {
                        await Service.AcknowledgeTaskCancellationAsync(task.Id, Lifetime.Token);
                        WriteLog(string.Format(L.T("Simulator acknowledged cancellation: {0}"), task.Id));
                    }
                    await RefreshResultsAsync(task);
                    if (SelectedTask?.Id == task.Id) SetProgressFields(Registry.GetTask(task.Id));
                }
                catch (Exception exception) { WriteLog(string.Format(L.T("Could not update simulation results: {0}"), exception)); }
                RefreshTasks();
                Notify();
            }
        }
    }

    private static ImageRect GetSimulationRegion(ImageRect scope, int index, int count)
    {
        var columns = (int)Math.Ceiling(Math.Sqrt(count));
        var rows = (int)Math.Ceiling((double)count / columns);
        var row = index / columns;
        var column = index % columns;
        var rowColumns = Math.Min(columns, count - row * columns);
        var left = scope.X + scope.Width * column / rowColumns;
        var top = scope.Y + scope.Height * row / rows;
        var right = column + 1 == rowColumns ? scope.Right : scope.X + scope.Width * (column + 1) / rowColumns;
        var bottom = row + 1 == rows ? scope.Bottom : scope.Y + scope.Height * (row + 1) / rows;
        return new ImageRect(left, top, right - left, bottom - top);
    }

    private async Task MoveSimulationViewportAsync(TilePyramidAgentTask task, ImageRect region, CancellationToken token)
    {
        if (!MoveViewportDuringSimulation
            || Registry.GetTask(task.Id).ControlState != TilePyramidAgentControlState.Agent) return;
        var viewport = await Service.GetViewportAsync(DocumentId, token);
        if (!viewport.IsInitialized || viewport.ViewWidth <= 0 || viewport.ViewHeight <= 0) return;
        var scale = Math.Min(viewport.ViewWidth / region.Width, viewport.ViewHeight / region.Height) * 0.8;
        if (viewport.MinimumScale is { } minimum) scale = Math.Max(minimum, scale);
        if (viewport.MaximumScale is { } maximum) scale = Math.Min(maximum, scale);
        try
        {
            await Service.SetViewportAsync(DocumentId, task.Id,
                new TilePyramidNavigationState(new ImagePoint(region.X + region.Width / 2, region.Y + region.Height / 2), scale, 0),
                task.FocalPlaneIndex, token);
        }
        catch (InvalidOperationException) when (Registry.GetTask(task.Id) is
               { State: TilePyramidAgentTaskState.Running, ControlState: not TilePyramidAgentControlState.Agent })
        {
            // An explicit control action may revoke authorization while SetViewportAsync is queued.
            WriteLog(L.T("Viewport control was released; simulated recognition continues in the background."));
        }
    }
}
