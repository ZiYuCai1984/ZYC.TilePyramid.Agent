using System.Windows;
using System.Windows.Input;

namespace ZYC.Framework.Modules.TilePyramid.UI;

internal partial class FloatPositionMenuView
{
    public static readonly DependencyProperty SetPositionCommandProperty = DependencyProperty.Register(
        nameof(SetPositionCommand),
        typeof(ICommand),
        typeof(FloatPositionMenuView));

    public static readonly DependencyProperty HideCommandProperty = DependencyProperty.Register(
        nameof(HideCommand),
        typeof(ICommand),
        typeof(FloatPositionMenuView));

    public FloatPositionMenuView()
    {
        InitializeComponent();
    }

    public ICommand SetPositionCommand
    {
        get => (ICommand)GetValue(SetPositionCommandProperty);
        set => SetValue(SetPositionCommandProperty, value);
    }


    public ICommand HideCommand
    {
        get => (ICommand)GetValue(HideCommandProperty);
        set => SetValue(HideCommandProperty, value);
    }
}