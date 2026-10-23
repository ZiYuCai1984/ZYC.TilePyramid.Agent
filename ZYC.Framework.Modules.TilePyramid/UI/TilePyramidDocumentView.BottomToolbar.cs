using System.Windows.Input;
using Autofac;
using ZYC.Framework.Modules.TilePyramid.Commands;
using ZYC.TilePyramid.Core.Viewports;
using ZYC.TilePyramid.Wpf;

namespace ZYC.Framework.Modules.TilePyramid.UI;

internal partial class TilePyramidDocumentView
{
    private AddFocalPlaneIndexCommand? _addFocalPlaneIndexCommand;

    private ICommand? _centerOnCommand;

    private ICommand? _fitToSourceCommand;

    private MinusFocalPlaneIndexCommand? _minusFocalPlaneIndexCommand;

    private ICommand? _resetRotationCommand;

    private ICommand? _rotateCommand;

    private ICommand? _zoomCommand;

    private ICommand? _zoomPresetCommand;

    public AddFocalPlaneIndexCommand AddFocalPlaneIndexCommand => _addFocalPlaneIndexCommand ??=
        LifetimeScope.Resolve<AddFocalPlaneIndexCommand>(
            new TypedParameter(typeof(TilePyramidDocumentView), this));

    public MinusFocalPlaneIndexCommand MinusFocalPlaneIndexCommand => _minusFocalPlaneIndexCommand ??=
        LifetimeScope.Resolve<MinusFocalPlaneIndexCommand>(
            new TypedParameter(typeof(TilePyramidDocumentView), this));

    public ICommand CenterOnCommand => _centerOnCommand ??=
        LifetimeScope.Resolve<CenterOnCommand>(
            new TypedParameter(typeof(TilePyramidDocumentView), this));

    public ICommand FitToSourceCommand => _fitToSourceCommand ??=
        LifetimeScope.Resolve<FitToSourceCommand>(
            new TypedParameter(typeof(TilePyramidView), MainView));

    public ICommand ZoomCommand => _zoomCommand ??=
        LifetimeScope.Resolve<ZoomCommand>(
            new TypedParameter(typeof(TilePyramidView), MainView));

    public ICommand RotateCommand => _rotateCommand ??=
        LifetimeScope.Resolve<RotateCommand>(
            new TypedParameter(typeof(TilePyramidView), MainView));

    public ICommand ResetRotationCommand => _resetRotationCommand ??=
        LifetimeScope.Resolve<ResetRotationCommand>(
            new TypedParameter(typeof(TilePyramidView), MainView));

    public ICommand ZoomPresetCommand => _zoomPresetCommand ??=
        LifetimeScope.Resolve<ZoomPresetCommand>(
            new TypedParameter(typeof(TilePyramidView), MainView));


    public int MaximumFocalPlaneIndex => Source.Info.FocalPlanes.Count - 1;

    public bool HasMultipleFocalPlanes => Source.Info.FocalPlanes.Count > 1;

    public int FocalPlaneIndex
    {
        get => MainView?.FocalPlaneIndex ?? Source.Info.DefaultFocalPlaneIndex;
        set => MainView.FocalPlaneIndex = value;
    }

    public bool CanIncreaseFocalPlane => FocalPlaneIndex < MaximumFocalPlaneIndex;

    public bool CanDecreaseFocalPlane => FocalPlaneIndex > 0;

    public int FocalPlaneNumber => FocalPlaneIndex + 1;

    public int FocalPlaneCount => Source.Info.FocalPlanes.Count;

    public string? FocalPlaneToolTip => Source.Info.GetFocalPlane(FocalPlaneIndex).PositionMicrometers is { } z
        ? $"Z = {z:+0.##;-0.##;0} μm"
        : null;

    private void MainView_OnFocalPlaneChanged(object? sender, EventArgs e)
    {
        UpdateFocalPlaneStatus();
        TabItem.NavigationRequests.OnNext(true);
    }

    private void UpdateFocalPlaneStatus()
    {
        DocumentState.FocalPlaneIndex = FocalPlaneIndex;

        OnPropertyChanged(nameof(FocalPlaneIndex));

        OnPropertyChanged(nameof(CanIncreaseFocalPlane));
        OnPropertyChanged(nameof(CanDecreaseFocalPlane));
        OnPropertyChanged(nameof(FocalPlaneNumber));
        OnPropertyChanged(nameof(FocalPlaneToolTip));

        AddFocalPlaneIndexCommand.RaiseCanExecuteChanged();
        MinusFocalPlaneIndexCommand.RaiseCanExecuteChanged();
    }

    public void CenterOn()
    {
        MainView.CenterOn(new ImagePoint(Source.Info.Width / 2d, Source.Info.Height / 2d));
    }
}
