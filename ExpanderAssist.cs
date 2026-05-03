using System.Windows;
using System.Windows.Input;

namespace SevenDaysVehicleEditor;

public static class ExpanderAssist
{
    public static readonly DependencyProperty ActionButtonContentProperty =
        DependencyProperty.RegisterAttached(
            "ActionButtonContent",
            typeof(object),
            typeof(ExpanderAssist),
            new PropertyMetadata(null));

    public static readonly DependencyProperty ActionButtonCommandProperty =
        DependencyProperty.RegisterAttached(
            "ActionButtonCommand",
            typeof(ICommand),
            typeof(ExpanderAssist),
            new PropertyMetadata(null));

    public static object? GetActionButtonContent(DependencyObject obj) =>
        obj.GetValue(ActionButtonContentProperty);

    public static void SetActionButtonContent(DependencyObject obj, object? value) =>
        obj.SetValue(ActionButtonContentProperty, value);

    public static ICommand? GetActionButtonCommand(DependencyObject obj) =>
        (ICommand?)obj.GetValue(ActionButtonCommandProperty);

    public static void SetActionButtonCommand(DependencyObject obj, ICommand? value) =>
        obj.SetValue(ActionButtonCommandProperty, value);
}
