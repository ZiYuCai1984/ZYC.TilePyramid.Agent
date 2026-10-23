using Autofac;
using ZYC.Framework.Core.Localizations;
using ZYC.Framework.Modules.TilePyramid.Abstractions;
using ZYC.Framework.Modules.TilePyramid.Abstractions.Agent;
using ZYC.Framework.Modules.TilePyramid.UI.AgentTasks;
using ZYC.TilePyramid.Annotations;
using ZYC.TilePyramid.Core.Viewports;

namespace ZYC.Framework.Modules.TilePyramid.UI;

internal partial class TilePyramidDocumentView
{
    public AgentTasksView? AgentTasksPanel { get; private set; }

    public string AgentTasksStatusText { get; private set; } = L.T("No tasks");

    private void InitializeAgentTasks()
    {
        var view = LifetimeScope.Resolve<AgentTasksView>(
            new TypedParameter(typeof(Guid), TabItem.TabReference.Id));

        AgentTasksPanel = view;
        OnPropertyChanged(nameof(AgentTasksPanel));
        // The toolbar keeps observing tasks even while their panel is hidden.
        AgentTasks.Changed += OnAgentTasksChanged;
        RefreshAgentTasksStatus();
    }

    private void DisposeAgentTasks()
    {
        AgentTasks.Changed -= OnAgentTasksChanged;
        AgentTasksPanel = null;
        OnPropertyChanged(nameof(AgentTasksPanel));
    }

    private void OnAgentTasksChanged(object? sender, EventArgs e)
    {
        RefreshAgentTasksStatus();
    }

    private void RefreshAgentTasksStatus()
    {
        var tasks = AgentTasks.GetTasks(TabItem.TabReference.Id);
        var controlling = tasks.Any(task => task.ControlState == TilePyramidAgentControlState.Agent);
        var requested = tasks.Count(task => task.ControlState == TilePyramidAgentControlState.Requested);
        var running = tasks.Count(task => task.State == TilePyramidAgentTaskState.Running);
        var stopping = tasks.Count(task => task.State == TilePyramidAgentTaskState.CancellationRequested);
        var failed = tasks.Count(task =>
            task.State is TilePyramidAgentTaskState.Failed or TilePyramidAgentTaskState.Interrupted);
        var pending = tasks.Sum(task => task.PendingCount);

        var status = new List<string>();
        if (controlling)
        {
            status.Add(L.T("Controlling the canvas"));
        }

        if (requested > 0)
        {
            status.Add(string.Format(L.T("Control requested {0}"), requested));
        }

        if (status.Count == 0)
        {
            if (running > 0)
            {
                status.Add(string.Format(L.T("Running {0}"), running));
            }

            if (stopping > 0)
            {
                status.Add(string.Format(L.T("Stopping {0}"), stopping));
            }

            if (failed > 0)
            {
                status.Add(string.Format(L.T("Errors {0}"), failed));
            }
        }

        if (status.Count == 0)
        {
            status.Add(pending > 0 ? string.Format(L.T("Pending review {0}"), pending) : tasks.Length > 0 ? L.T("Ended") : L.T("No tasks"));
        }

        var statusText = string.Join(" · ", status);
        if (AgentTasksStatusText != statusText)
        {
            AgentTasksStatusText = statusText;
            OnPropertyChanged(nameof(AgentTasksStatusText));
        }
    }

    internal void ShowAgentTaskResults(Guid batchId)
    {
        SynchronizeRecognitionList();
        var option = RecognitionBatches.FirstOrDefault(item => item.Batch?.Id == batchId)
                     ?? throw new InvalidOperationException(L.T("The recognition batch no longer exists."));
        SelectedRecognitionBatch = option;
        var features = option.Batch!.Features;
        SetRecognitionListMode(
            features.Count == 0 || features.Any(feature => feature.Decision == ReviewDecision.Pending)
                ? ReviewDecision.Pending
                : features.Any(feature => feature.Decision == ReviewDecision.Accepted)
                    ? ReviewDecision.Accepted
                    : ReviewDecision.Rejected);
        DocumentState.FloatingPanels[FloatingPanelIds.Recognition].IsVisible = true;
    }

    internal TilePyramidAgentViewport GetAgentViewport()
    {
        var initialized = MainView.IsViewInitialized;
        return new TilePyramidAgentViewport(TabItem.TabReference.Id,
            MainView.NavigationState, MainView.FocalPlaneIndex, initialized, IsVisible,
            MainView.ActualWidth, MainView.ActualHeight,
            initialized ? MainView.Metrics.VisibleWorldQuad : null,
            initialized ? MainView.Metrics.MinimumScale : null,
            initialized ? MainView.Metrics.MaximumScale : null);
    }

    internal void SetAgentViewport(TilePyramidNavigationState navigation, int focalPlane)
    {
        EnsureAgentWriteAllowed();
        MainView.FocalPlaneIndex = focalPlane;
        MainView.NavigationState = navigation;
        DocumentState.Navigation = MainView.NavigationState;
        DocumentState.FocalPlaneIndex = MainView.FocalPlaneIndex;
        // The service commits this navigation explicitly, including changes to the focal plane.
        TabItem.NavigationRequests.OnNext(false);
    }

    internal void EnsureAgentWriteAllowed()
    {
        if (_disposed || IsEditingAnnotation)
        {
            throw new InvalidOperationException("The document is closed or an annotation is being edited.");
        }
    }
}
