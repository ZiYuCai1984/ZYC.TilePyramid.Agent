using System.ComponentModel;
using System.Runtime.CompilerServices;
using ZYC.TilePyramid.Wpf;

namespace ZYC.Framework.Modules.TilePyramid;

internal partial class TilePyramidTabItem : INotifyPropertyChanged
{
    /// <summary>Runtime diagnostics for this tab; never included in persisted document state.</summary>
    public TilePyramidDiagnostics? Diagnostics { get; private set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    internal void UpdateDiagnostics(TilePyramidDiagnostics diagnostics)
    {
        if (Diagnostics == diagnostics)
        {
            return;
        }

        Diagnostics = diagnostics;
        OnPropertyChanged(nameof(Diagnostics));
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}