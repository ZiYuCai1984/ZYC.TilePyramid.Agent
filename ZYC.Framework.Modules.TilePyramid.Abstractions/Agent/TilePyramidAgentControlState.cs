namespace ZYC.Framework.Modules.TilePyramid.Abstractions.Agent;

/// <summary>Control of the task's document viewport. Only the local UI can grant control.</summary>
public enum TilePyramidAgentControlState
{
    /// <summary>The user controls the viewport; recognition can run in the background.</summary>
    User,
    /// <summary>The agent requested control and is waiting for the user to grant it.</summary>
    Requested,
    /// <summary>The user granted this task exclusive viewport control.</summary>
    Agent,
    /// <summary>The user reclaimed or declined control. Only the UI can grant it again.</summary>
    Revoked
}
