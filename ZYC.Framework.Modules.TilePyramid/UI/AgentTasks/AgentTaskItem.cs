using System.ComponentModel;
using ZYC.Framework.Core.Localizations;
using ZYC.Framework.Modules.TilePyramid.Abstractions.Agent;

namespace ZYC.Framework.Modules.TilePyramid.UI.AgentTasks;

internal class AgentTaskItem : INotifyPropertyChanged
{
    private bool _isDetailsExpanded;

    public AgentTaskItem(TilePyramidAgentTask snapshot)
    {
        Snapshot = snapshot;
    }

    public TilePyramidAgentTask Snapshot { get; private set; }
    public Guid Id => Snapshot.Id;
    public bool CanStop => Snapshot.State == TilePyramidAgentTaskState.Running;
    public bool ShowControlState => CanStop;

    public bool IsDetailsExpanded
    {
        get => _isDetailsExpanded;
        set
        {
            if (_isDetailsExpanded == value)
            {
                return;
            }

            _isDetailsExpanded = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsDetailsExpanded)));
        }
    }

    public bool CanGrantControl => CanStop && Snapshot.IsDocumentAvailable
                                           && Snapshot.ControlState is TilePyramidAgentControlState.Requested
                                               or TilePyramidAgentControlState.Revoked;

    public bool CanReclaimControl => CanStop
                                     && Snapshot.ControlState is TilePyramidAgentControlState.Agent
                                         or TilePyramidAgentControlState.Requested;

    public string ReclaimText => Snapshot.ControlState == TilePyramidAgentControlState.Requested
        ? L.T("Decline Control") : L.T("Reclaim Control");

    public string GrantText => Snapshot.ControlState == TilePyramidAgentControlState.Revoked
        ? L.T("Grant Control Again") : L.T("Grant Control");

    public string ExecutionStateText => Snapshot.State switch
    {
        TilePyramidAgentTaskState.Running => L.T("Running"),
        TilePyramidAgentTaskState.CancellationRequested => L.T("Stopping"),
        TilePyramidAgentTaskState.Completed => L.T("Completed"),
        TilePyramidAgentTaskState.Cancelled => L.T("Stopped"),
        TilePyramidAgentTaskState.Failed => L.T("Recognition failed"),
        _ => L.T("Task interrupted")
    };

    public string ControlStateText => Snapshot.ControlState switch
    {
        TilePyramidAgentControlState.Agent => L.T("Agent controls the canvas"),
        TilePyramidAgentControlState.Requested => L.T("Canvas control requested"),
        _ => L.T("Recognizing in the background")
    };

    public string StatusText => ShowControlState
        ? $"{ExecutionStateText} · {ControlStateText}"
        : ExecutionStateText;

    public string ActivityText => Snapshot.State switch
    {
        TilePyramidAgentTaskState.Running => Snapshot.Stage,
        TilePyramidAgentTaskState.CancellationRequested => L.T("Waiting for the executor to confirm cancellation; submitted results will be retained"),
        _ => Snapshot.Message ?? L.T("The task has ended; existing results are available")
    };

    public string ProgressText => Snapshot.TotalRegions is { } total
        ? string.Format(L.T("Processed {0} / {1} regions"), Snapshot.CompletedRegions, total)
        : string.Format(L.T("Processed {0} regions · total unknown"), Snapshot.CompletedRegions);

    public string ResultsText =>
        string.Format(L.T("Pending {0} · accepted {1} · rejected {2}"),
            Snapshot.PendingCount, Snapshot.AcceptedCount, Snapshot.RejectedCount);

    public string ReviewPolicyText => Snapshot.AutoAcceptRecognitionResults
        ? L.T("Result handling: accept automatically and create annotations (fixed at task start)")
        : L.T("Result handling: manual review (fixed at task start)");

    public string ScopeText =>
        string.Format(L.T("Plane {0} · Region ({1:G6}, {2:G6}) {3:G6} × {4:G6} · {5}"),
            Snapshot.FocalPlaneIndex + 1, Snapshot.Region.X, Snapshot.Region.Y,
            Snapshot.Region.Width, Snapshot.Region.Height, Snapshot.LayerName);

    public string TimingText =>
        string.Format(L.T("Started {0:HH:mm:ss} · Last response {1:HH:mm:ss}"),
            Snapshot.StartedAt.ToLocalTime(), Snapshot.LastHeartbeatAt.ToLocalTime());

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Update(TilePyramidAgentTask snapshot)
    {
        if (Snapshot == snapshot)
        {
            return;
        }

        Snapshot = snapshot;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
    }
}
