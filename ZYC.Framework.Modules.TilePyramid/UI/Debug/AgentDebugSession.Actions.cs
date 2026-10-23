using ZYC.Framework.Core.Localizations;
using ZYC.Framework.Modules.TilePyramid.Abstractions.Agent;
using ZYC.TilePyramid.Core.Viewports;

namespace ZYC.Framework.Modules.TilePyramid.UI.Debug;

internal partial class AgentDebugSession
{
    public async Task ExecuteAsync(string action)
    {
        if (_disposed) return;
        // Cancellation and control actions remain available during image I/O.
        var immediate = action is "Stop" or "StopSimulation" or "Fail" or "RequestControl" or "GrantControl"
            or "ReclaimControl" or "ReleaseControl" or "ShowResults" or "ClearLog";
        if (_busy && !immediate)
        {
            WriteLog(L.T("Wait for the current API call to finish. Stop and viewport control actions remain available."));
            return;
        }
        if (!immediate)
        {
            _busy = true;
            _operationTaskId = SelectedTask?.Id;
            Notify(nameof(Activity));
        }
        try
        {
            var task = SelectedTask is null ? null : RequireTask();
            if (task?.Id == _simulationTaskId && _simulationTaskId is not null
                && action is "Submit" or "ReadRegion" or "ReportProgress" or "Complete" or "Acknowledge")
                throw new InvalidOperationException(L.T("This task is running automatically. Stop the simulator before reading, writing or acknowledging cancellation manually."));

            switch (action)
            {
                case "Initialize":
                case "Refresh":
                    await RefreshLayersAsync();
                    if (action == "Initialize") await FillScopeAsync(true);
                    RefreshTasks();
                    if (SelectedTask is not null) SetProgressFields(RequireTask());
                    break;
                case "WholeImage":
                    await FillScopeAsync(false);
                    break;
                case "VisibleRegion":
                    await FillScopeAsync(true);
                    break;
                case "CreateLayer":
                    var layer = await Service.CreateLayerAsync(DocumentId, LayerName, Lifetime.Token);
                    await RefreshLayersAsync();
                    SelectedLayer = Layers.First(item => item.Id == layer.Id);
                    break;
                case "Create":
                    task = await CreateTaskAsync();
                    break;
                case "Simulate":
                    if (_simulationTaskId is not null) throw new InvalidOperationException(L.T("The local simulator is running. Stop it or wait for it to finish."));
                    var count = Integer(SimulationRegions, L.T("Region count"), 1, 1024);
                    var delay = Integer(SimulationDelay, L.T("Delay per region (ms)"), 0, 60000);
                    var score = Number(CandidateScore, L.T("Candidate score"));
                    var label = CandidateLabel;
                    var shape = CandidateShape;
                    task = await CreateTaskAsync();
                    StartSimulation(task, count, delay, label, score, shape);
                    break;
                case "ReadRegion":
                    task = RequireTask();
                    var region = await ReadAsync(task, task.Region, Lifetime.Token);
                    SetPreview(task.Id, region);
                    break;
                case "NewCandidate":
                    CandidateId = Guid.NewGuid().ToString();
                    break;
                case "Submit":
                    task = RequireTask();
                    var crop = _crop;
                    var bounds = crop is null ? task.Region : new ImageRect(0, 0, crop.PixelWidth, crop.PixelHeight);
                    var candidate = CreateCandidate(task, Guid.Parse(CandidateId), bounds, CandidateLabel,
                        Number(CandidateScore, L.T("Candidate score")), CandidateShape);
                    var submitted = await Service.SubmitCandidatesAsync(DocumentId, task.BatchId, [candidate],
                        crop?.CropId, Lifetime.Token);
                    WriteLog(string.Format(L.T("Submission result: {0}"), submitted));
                    await RefreshResultsAsync(task);
                    break;
                case "ReportProgress":
                    await Service.ReportTaskProgressAsync(RequireTask().Id, Stage,
                        Integer(CompletedRegions, L.T("Completed count")),
                        string.IsNullOrWhiteSpace(TotalRegions) ? null : Integer(TotalRegions, L.T("Total count")), Lifetime.Token);
                    break;
                case "Heartbeat":
                    await Service.ReportTaskProgressAsync(RequireTask().Id, cancellationToken: Lifetime.Token);
                    break;
                case "Complete":
                    await Service.EndRecognitionAsync(DocumentId, RequireTask().BatchId, Lifetime.Token);
                    break;
                case "Stop":
                    await Service.RequestTaskCancellationAsync(RequireTask().Id, Lifetime.Token);
                    break;
                case "StopSimulation":
                    if (_simulationTaskId is not { } runningId) throw new InvalidOperationException(L.T("No local simulation task is running."));
                    await Service.RequestTaskCancellationAsync(runningId, Lifetime.Token);
                    break;
                case "Acknowledge":
                    await Service.AcknowledgeTaskCancellationAsync(RequireTask().Id, Lifetime.Token);
                    break;
                case "Fail":
                    await Service.FailTaskAsync(RequireTask().Id, FailureMessage, Lifetime.Token);
                    break;
                case "RequestControl":
                    await Service.RequestViewportControlAsync(RequireTask().Id, Lifetime.Token);
                    break;
                case "GrantControl":
                    Registry.GrantControl(RequireTask().Id);
                    break;
                case "ReclaimControl":
                    Registry.ReclaimControl(DocumentId);
                    break;
                case "ReleaseControl":
                    await Service.ReleaseViewportControlAsync(RequireTask().Id, Lifetime.Token);
                    break;
                case "GetViewport":
                    SetViewportFields(await Service.GetViewportAsync(DocumentId, Lifetime.Token));
                    break;
                case "SetViewport":
                    task = RequireTask();
                    var navigation = new TilePyramidNavigationState(
                        new ImagePoint(Number(CenterX, L.T("Center X")), Number(CenterY, L.T("Center Y"))),
                        Number(Scale, L.T("Zoom")), Number(Rotation, L.T("Rotation")));
                    SetViewportFields(await Service.SetViewportAsync(DocumentId, task.Id, navigation,
                        task.FocalPlaneIndex, Lifetime.Token));
                    break;
                case "ShowResults":
                    ShowResults(RequireTask().BatchId);
                    break;
                case "GetResults":
                    await RefreshResultsAsync(RequireTask());
                    break;
                case "Accept":
                case "Reject":
                case "ResetReview":
                case "CreateAnnotation":
                    task = RequireTask();
                    var featureId = Guid.Parse(CandidateId);
                    if (action == "Accept") await Service.AcceptRecognitionAsync(DocumentId, task.BatchId, featureId, Lifetime.Token);
                    else if (action == "Reject") await Service.RejectRecognitionAsync(DocumentId, task.BatchId, featureId, Lifetime.Token);
                    else if (action == "ResetReview") await Service.ResetRecognitionReviewAsync(DocumentId, task.BatchId, featureId, Lifetime.Token);
                    else await Service.CreateRecognitionAnnotationAsync(DocumentId, task.BatchId, featureId, Lifetime.Token);
                    await RefreshResultsAsync(task);
                    break;
                case "ClearLog":
                    LogEntries.Clear();
                    break;
                default:
                    throw new ArgumentException(string.Format(L.T("Unknown debug action: {0}"), action));
            }
            WriteLog(string.Format(L.T("{0} completed"), action) + (task is null ? "" : $" · {task.Id}"));
        }
        catch (OperationCanceledException) { WriteLog(string.Format(L.T("{0} cancelled"), action)); }
        catch (Exception exception) { WriteLog(string.Format(L.T("{0} failed: {1}"), action, exception)); }
        finally
        {
            if (!immediate)
            {
                _busy = false;
                _operationTaskId = null;
            }
            if (!_disposed)
            {
                RefreshTasks();
                Notify();
            }
        }
    }

    private async Task RefreshLayersAsync()
    {
        var selectedId = SelectedLayer?.Id;
        var layers = await Service.GetLayersAsync(DocumentId, Lifetime.Token);
        Layers.Clear();
        foreach (var layer in layers) Layers.Add(layer);
        SelectedLayer = Layers.FirstOrDefault(layer => layer.Id == selectedId)
                        ?? Layers.FirstOrDefault(layer => !layer.IsLocked);
    }

    private async Task FillScopeAsync(bool visible)
    {
        var document = await Service.GetDocumentInfoAsync(DocumentId, Lifetime.Token);
        var region = new ImageRect(0, 0, document.Info.Width, document.Info.Height);
        if (visible)
        {
            var viewport = await Service.GetViewportAsync(DocumentId, Lifetime.Token);
            if (viewport.VisibleQuad is { } quad)
            {
                var bounds = quad.Bounds;
                var left = Math.Max(0, bounds.Left);
                var top = Math.Max(0, bounds.Top);
                var right = Math.Min(document.Info.Width, bounds.Right);
                var bottom = Math.Min(document.Info.Height, bounds.Bottom);
                if (right > left && bottom > top) region = new ImageRect(left, top, right - left, bottom - top);
                else throw new InvalidOperationException(L.T("The current viewport does not intersect the image."));
            }
            else WriteLog(L.T("The viewport is not ready. Using the whole image."));
        }
        RegionX = Format(region.X);
        RegionY = Format(region.Y);
        RegionWidth = Format(region.Width);
        RegionHeight = Format(region.Height);
        FocalPlane = (document.FocalPlaneIndex + 1).ToString();
    }

    private async Task<TilePyramidAgentTask> CreateTaskAsync()
    {
        var region = new ImageRect(Number(RegionX, "X"), Number(RegionY, "Y"),
            Number(RegionWidth, L.T("Width")), Number(RegionHeight, L.T("Height")));
        var batch = await Service.BeginRecognitionAsync(DocumentId, Model, Title, region,
            Integer(FocalPlane, L.T("Focal Plane"), 1) - 1,
            SelectedLayer?.Id ?? throw new InvalidOperationException(L.T("Select a target layer.")), Lifetime.Token);
        var id = batch.TaskId ?? throw new InvalidOperationException(L.T("Creating the batch did not return a task ID."));
        OwnedTaskIds.Add(id);
        _operationTaskId = id;
        RefreshTasks();
        SelectedTask = Tasks.First(item => item.Id == id);
        WriteLog(string.Format(L.T("Created local task: {0}; batch: {1}"), id, batch.Id));
        return Registry.GetTask(id);
    }
}
