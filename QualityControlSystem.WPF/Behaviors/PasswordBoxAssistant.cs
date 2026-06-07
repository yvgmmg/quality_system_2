using System.Windows;
using System.Windows.Controls;

namespace QualityControlSystem.WPF.Behaviors;

public static class PasswordBoxAssistant
{
    public static readonly DependencyProperty BoundPasswordProperty =
        DependencyProperty.RegisterAttached(
            "BoundPassword",
            typeof(string),
            typeof(PasswordBoxAssistant),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnBoundPasswordChanged));

    private static readonly DependencyProperty IsUpdatingProperty =
        DependencyProperty.RegisterAttached(
            "IsUpdating",
            typeof(bool),
            typeof(PasswordBoxAssistant));

    public static string GetBoundPassword(DependencyObject obj)
    {
        return obj.GetValue(BoundPasswordProperty) as string ?? string.Empty;
    }

    public static void SetBoundPassword(DependencyObject obj, string value)
    {
        obj.SetValue(BoundPasswordProperty, value);
    }

    private static bool GetIsUpdating(DependencyObject obj)
    {
        return (bool)obj.GetValue(IsUpdatingProperty);
    }

    private static void SetIsUpdating(DependencyObject obj, bool value)
    {
        obj.SetValue(IsUpdatingProperty, value);
    }

    private static void OnBoundPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not PasswordBox passwordBox || GetIsUpdating(passwordBox))
        {
            return;
        }

        passwordBox.PasswordChanged -= OnPasswordChanged;
        passwordBox.Password = e.NewValue as string ?? string.Empty;
        passwordBox.PasswordChanged += OnPasswordChanged;
    }

    private static void OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is not PasswordBox passwordBox)
        {
            return;
        }

        SetIsUpdating(passwordBox, true);
        SetBoundPassword(passwordBox, passwordBox.Password);
        SetIsUpdating(passwordBox, false);
    }
}
