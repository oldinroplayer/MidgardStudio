using System;
using System.Collections.Generic;
using System.Windows;
using Wpf.Ui.Controls;

namespace MidgardStudio.App.Views;

/// <summary>Modal that picks a value from a reference database with live autocomplete (e.g. choose which mob a
/// mob_avail entry disguises). Returns the typed/picked name, or null on cancel.</summary>
public partial class ReferenceInputDialog : FluentWindow
{
    private readonly Func<string, IReadOnlyList<string>> _search;

    public ReferenceInputDialog(string title, string prompt, Func<string, IReadOnlyList<string>> search, string initial = "")
    {
        InitializeComponent();
        Title = title;
        TitleBarCtl.Title = title;
        PromptText.Text = prompt;
        _search = search;
        NameBox.Text = initial;

        Loaded += (_, _) => { NameBox.Focus(); Refresh(); };
        NameBox.TextChanged += OnTextChanged;
        SuggestList.SelectionChanged += (_, _) => { if (SuggestList.SelectedItem is string s) SetText(s); };
        SuggestList.MouseDoubleClick += (_, _) => { if (SuggestList.SelectedItem is string) Confirm(); };
        OkButton.Click += (_, _) => Confirm();
        CancelButton.Click += (_, _) => { DialogResult = false; Close(); };
    }

    public string Value { get; private set; } = string.Empty;

    private void SetText(string s)
    {
        NameBox.TextChanged -= OnTextChanged; // avoid a refresh that would clear the just-made selection
        NameBox.Text = s;
        NameBox.CaretIndex = s.Length;
        NameBox.TextChanged += OnTextChanged;
    }

    private void OnTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) => Refresh();

    private void Refresh()
    {
        try { SuggestList.ItemsSource = _search(NameBox.Text ?? string.Empty); }
        catch { SuggestList.ItemsSource = Array.Empty<string>(); }
    }

    private void Confirm()
    {
        var v = (NameBox.Text ?? string.Empty).Trim();
        if (v.Length == 0)
        {
            ErrorText.Text = "Pick a value.";
            ErrorText.Visibility = Visibility.Visible;
            return;
        }
        Value = v;
        DialogResult = true;
        Close();
    }
}
