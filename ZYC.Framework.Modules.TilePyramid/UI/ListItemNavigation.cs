using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ZYC.Framework.Modules.TilePyramid.UI;

// A fresh request lets repeated clicks reveal the same item after the user has scrolled elsewhere.
internal class ListItemNavigationRequest
{
    public ListItemNavigationRequest(object item)
    {
        Item = item;
    }

    public object Item { get; }
    public bool IsHandled { get; set; }
}

internal static class ListItemNavigation
{
    public static readonly DependencyProperty RequestProperty = DependencyProperty.RegisterAttached(
        "Request", typeof(ListItemNavigationRequest), typeof(ListItemNavigation),
        new PropertyMetadata(null, OnRequestChanged));

    public static ListItemNavigationRequest? GetRequest(DependencyObject element)
    {
        return (ListItemNavigationRequest?)element.GetValue(RequestProperty);
    }

    public static void SetRequest(DependencyObject element, ListItemNavigationRequest? value)
    {
        element.SetValue(RequestProperty, value);
    }

    private static void OnRequestChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ListBox list)
        {
            return;
        }

        StopWaiting(list);
        if (e.NewValue is not ListItemNavigationRequest { IsHandled: false })
        {
            return;
        }

        // Hidden panels may be loaded after the navigation request is bound.
        list.Loaded += OnLoaded;
        list.IsVisibleChanged += OnIsVisibleChanged;
        list.SizeChanged += OnSizeChanged;
        ScheduleNavigation(list);
    }

    private static void OnLoaded(object sender, RoutedEventArgs e)
    {
        ScheduleNavigation((ListBox)sender);
    }

    private static void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is true)
        {
            ScheduleNavigation((ListBox)sender);
        }
    }

    private static void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        ScheduleNavigation((ListBox)sender);
    }

    private static void ScheduleNavigation(ListBox list)
    {
        if (!list.IsLoaded || !list.IsVisible || GetRequest(list) is not { IsHandled: false } request)
        {
            return;
        }

        // Run after panel visibility, filters and item containers have caught up with their bindings.
        list.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
        {
            if (!ReferenceEquals(GetRequest(list), request) || request.IsHandled || !list.IsLoaded || !list.IsVisible)
            {
                return;
            }

            if (list.ActualWidth <= 0 || list.ActualHeight <= 0)
            {
                return;
            }

            request.IsHandled = true;
            StopWaiting(list);
            if (!list.Items.Contains(request.Item))
            {
                return;
            }

            list.SelectedItem = request.Item;
            list.ScrollIntoView(request.Item);
            list.UpdateLayout();
            if (list.ItemContainerGenerator.ContainerFromItem(request.Item) is ListBoxItem container)
            {
                container.BringIntoView();
                container.Focus();
            }
        }));
    }

    private static void StopWaiting(ListBox list)
    {
        list.Loaded -= OnLoaded;
        list.IsVisibleChanged -= OnIsVisibleChanged;
        list.SizeChanged -= OnSizeChanged;
    }
}