using Autofac;
using ZYC.Framework.Modules.TilePyramid.Abstractions.Agent;
using ZYC.Framework.Modules.TilePyramid.UI.Debug;

namespace ZYC.Framework.Modules.TilePyramid.UI;

internal partial class TilePyramidDocumentView
{
    private AgentDebugSession? _agentDebug;

    public AgentDebugSession AgentDebug => _agentDebug ??= new AgentDebugSession(
        TabItem.TabReference.Id, AgentTasks, LifetimeScope.Resolve<ITilePyramidAgentService>(), ShowAgentTaskResults);
}
