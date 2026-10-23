using PropertyChanged;
using ZYC.CoreToolkit.Abstractions.Settings;
using ZYC.Framework.Abstractions.Config.Attributes;

namespace ZYC.Framework.Modules.TilePyramid.Abstractions;

/// <summary>Stores global view preferences and document snapshots using Framework persistence.</summary>
[SkipReset]
[AddINotifyPropertyChangedInterface]
public class TilePyramidState : IState
{
    /// <summary>
    ///     Gets or sets snapshots keyed by canonical document URI without viewport parameters.
    ///     Local document snapshots are stored beside their source files instead.
    /// </summary>
    public Dictionary<string, TilePyramidDocumentState> Documents { get; set; } = new();

}