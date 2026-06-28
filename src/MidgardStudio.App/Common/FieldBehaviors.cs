using System.Windows;
using System.Windows.Input;

namespace MidgardStudio.App.Common;

/// <summary>Attached behaviors for the schema-generated field editors.</summary>
public static class FieldBehaviors
{
    /// <summary>Runs the bound command when the element loses keyboard focus. The reference field's editable
    /// combo uses this to commit its live query text to the record once (on blur) instead of per keystroke —
    /// so the dropdown can filter live (Text bound PropertyChanged) without spamming the undo stack.</summary>
    public static readonly DependencyProperty CommitOnLostFocusProperty =
        DependencyProperty.RegisterAttached(
            "CommitOnLostFocus", typeof(ICommand), typeof(FieldBehaviors), new PropertyMetadata(null, OnChanged));

    public static void SetCommitOnLostFocus(DependencyObject d, ICommand? value) => d.SetValue(CommitOnLostFocusProperty, value);

    public static ICommand? GetCommitOnLostFocus(DependencyObject d) => (ICommand?)d.GetValue(CommitOnLostFocusProperty);

    private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement fe) return;
        fe.LostKeyboardFocus -= OnLostFocus;
        if (e.NewValue is ICommand) fe.LostKeyboardFocus += OnLostFocus;
    }

    private static void OnLostFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is FrameworkElement fe && GetCommitOnLostFocus(fe) is { } cmd && cmd.CanExecute(null))
            cmd.Execute(null);
    }
}
