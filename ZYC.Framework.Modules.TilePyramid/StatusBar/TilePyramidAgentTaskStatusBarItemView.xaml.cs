using System.ComponentModel;
using Autofac;
using ZYC.CoreToolkit.Extensions.Autofac.Attributes;
using ZYC.Framework.Core.Localizations;
using ZYC.Framework.Modules.TilePyramid.Abstractions.Agent;
using ZYC.Framework.Modules.TilePyramid.UI.AgentTasks;

namespace ZYC.Framework.Modules.TilePyramid.StatusBar;

[RegisterSingleInstance]
internal partial class TilePyramidAgentTaskStatusBarItemView : INotifyPropertyChanged, IDisposable
{
    public TilePyramidAgentTaskStatusBarItemView(TilePyramidAgentTasks tasks, ILifetimeScope lifetimeScope)
    {
        Tasks = tasks;
        InitializeComponent();
        TaskList = lifetimeScope.Resolve<AgentTasksView>();
        TaskList.ResultsOpened += OnResultsOpened;
        TaskListHost.Content = TaskList;
        Tasks.Changed += OnTasksChanged;
        Refresh();
    }

    private TilePyramidAgentTasks Tasks { get; }
    private AgentTasksView TaskList { get; }
    public bool HasTasks { get; private set; }
    public string Summary { get; private set; } = L.T("Agent Tasks");
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnTasksChanged(object? sender, EventArgs e) => Refresh();
    private void OnResultsOpened(object? sender, EventArgs e) => TasksToggle.IsChecked = false;

    private void Refresh()
    {
        var tasks = Tasks.GetTasks();
        var running = tasks.Count(task => task.State == TilePyramidAgentTaskState.Running);
        var stopping = tasks.Count(task => task.State == TilePyramidAgentTaskState.CancellationRequested);
        var attention = tasks.Count(task => task.State is TilePyramidAgentTaskState.Failed or TilePyramidAgentTaskState.Interrupted
            || task.ControlState == TilePyramidAgentControlState.Requested);
        HasTasks = tasks.Length != 0;
        Summary = string.Format(
            L.T("Agent Tasks · {0} running · {1} stopping · {2} need attention · {3} pending review"),
            running, stopping, attention, tasks.Sum(task => task.PendingCount));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
    }

    public void Dispose()
    {
        Tasks.Changed -= OnTasksChanged;
        TaskList.ResultsOpened -= OnResultsOpened;
        TasksToggle.IsChecked = false;
        TaskListHost.Content = null;
    }
}
