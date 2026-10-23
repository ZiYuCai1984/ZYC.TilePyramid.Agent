using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using Microsoft.Extensions.Logging;
using ZYC.CoreToolkit.Extensions.Autofac.Attributes;
using ZYC.Framework.Abstractions;
using ZYC.Framework.Abstractions.Notification.Toast;
using ZYC.Framework.Abstractions.Tab;
using ZYC.Framework.Abstractions.Workspace;
using ZYC.Framework.Core.Localizations;

namespace ZYC.Framework.Modules.TilePyramid.UI.AgentTasks;

[Register]
internal partial class AgentTasksView : INotifyPropertyChanged
{
    public AgentTasksView(
        TilePyramidAgentTasks tasks,
        TilePyramidAgentDocuments documents,
        ITabManager tabManager,
        IParallelWorkspaceManager workspaceManager,
        IToastManager toastManager,
        ILogger<AgentTasksView> logger)
        : this(null, tasks, documents, tabManager, workspaceManager, toastManager, logger)
    {
    }

    public AgentTasksView(
        Guid? documentId,
        TilePyramidAgentTasks tasks,
        TilePyramidAgentDocuments documents,
        ITabManager tabManager,
        IParallelWorkspaceManager workspaceManager,
        IToastManager toastManager,
        ILogger<AgentTasksView> logger)
    {
        DocumentId = documentId;
        Tasks = tasks;
        Documents = documents;
        TabManager = tabManager;
        WorkspaceManager = workspaceManager;
        ToastManager = toastManager;
        Logger = logger;

        InitializeComponent();
    }

    private TilePyramidAgentTasks Tasks { get; }
    private TilePyramidAgentDocuments Documents { get; }
    private ITabManager TabManager { get; }
    private IParallelWorkspaceManager WorkspaceManager { get; }
    private IToastManager ToastManager { get; }
    private ILogger<AgentTasksView> Logger { get; }

    public Guid? DocumentId { get; }

    public ObservableCollection<AgentTaskItem> Items { get; } = new();

    public bool HasTasks => Items.Count != 0;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? ResultsOpened;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Tasks.Changed -= OnTasksChanged;
        Tasks.Changed += OnTasksChanged;
        Refresh();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        Tasks.Changed -= OnTasksChanged;
    }

    private void OnTasksChanged(object? sender, EventArgs e)
    {
        Refresh();
    }

    private void Refresh()
    {
        var snapshots = Tasks.GetTasks(DocumentId);
        foreach (var snapshot in snapshots.Reverse())
        {
            var item = Items.FirstOrDefault(item => item.Id == snapshot.Id);
            if (item is null)
            {
                Items.Insert(0, new AgentTaskItem(snapshot));
            }
            else
            {
                item.Update(snapshot);
            }
        }

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
    }

    private void RunAction(object sender, Action<Guid> action)
    {
        if (sender is not FrameworkElement { Tag: Guid taskId })
        {
            return;
        }

        try
        {
            action(taskId);
        }
        catch (Exception exception)
        {
            Logger.Error(exception);
            ToastManager.PromptException(exception);
        }
    }

    private void Stop_OnClick(object sender, RoutedEventArgs e)
    {
        RunAction(sender, id => Tasks.RequestCancellation(id));
    }

    private void GrantControl_OnClick(object sender, RoutedEventArgs e)
    {
        RunAction(sender, Tasks.GrantControl);
    }

    private void ReclaimControl_OnClick(object sender, RoutedEventArgs e)
    {
        RunAction(sender, id => Tasks.ReclaimControl(Tasks.GetTask(id).DocumentId));
    }

    private void Results_OnClick(object sender, RoutedEventArgs e)
    {
        RunAction(sender, id =>
        {
            var task = Tasks.GetTask(id);
            var tab = Documents.Items.FirstOrDefault(tab => tab.TabReference.Id == task.DocumentId)
                      ?? throw new InvalidOperationException(L.T("The target tab is closed. Reopen the image to view saved results."));
            var workspace = TabManager.GetTabItemInstanceWorkspace(tab);
            WorkspaceManager.SetFocusedWorkspace(workspace);
            TabManager.SetFocusedTabItemInstance(workspace.Id, tab);
            ((TilePyramidDocumentView)tab.View).ShowAgentTaskResults(task.BatchId);
            ResultsOpened?.Invoke(this, EventArgs.Empty);
        });
    }

    private void Details_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var item = (AgentTaskItem)((FrameworkElement)sender).Tag;
            item.IsDetailsExpanded = !item.IsDetailsExpanded;
        }
        catch (Exception ex)
        {
            ToastManager.PromptException(ex);
            Logger.Error(ex);
        }
    }
}
