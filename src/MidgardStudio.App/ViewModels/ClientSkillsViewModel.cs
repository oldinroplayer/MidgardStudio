using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MidgardStudio.App.Common;
using MidgardStudio.App.Services;
using MidgardStudio.Core.Lua;
using MidgardStudio.Core.Model;

namespace MidgardStudio.App.ViewModels;

/// <summary>A row in the Client Skills master list (reads through the live <see cref="ClientSkill"/>).</summary>
public sealed class ClientSkillRowViewModel : ObservableObject
{
    public ClientSkillRowViewModel(ClientSkill skill, bool isCustom) { Skill = skill; IsCustom = isCustom; }

    public ClientSkill Skill { get; }
    public bool IsCustom { get; }
    public string Constant => Skill.Constant;
    public int Id => Skill.Id;
    public string Name => Skill.SkillName.Length > 0 ? Skill.SkillName : Skill.Constant;

    public bool Matches(string q) =>
        Constant.Contains(q, StringComparison.OrdinalIgnoreCase)
        || Name.Contains(q, StringComparison.OrdinalIgnoreCase)
        || Id.ToString().Contains(q, StringComparison.Ordinal);

    public void Refresh() => OnPropertyChanged(string.Empty);
}

/// <summary>
/// The Client Skills section: a standalone master list of client skills (from skillid.lub) with a detail
/// editor over the three skill tables. Independent of the server skill_db; create + edit custom skills.
/// </summary>
public sealed partial class ClientSkillsViewModel : ObservableObject, IDisposable
{
    private readonly WorkspaceSession _session;
    private readonly ClientSkillService _service;
    private readonly Action<string, RecordKey>? _navigate;
    private List<ClientSkillRowViewModel> _all = new();
    private RecordKey? _pendingSelect;

    public ClientSkillsViewModel(WorkspaceSession session, ClientSkillService service, Action<string, RecordKey>? navigate = null)
    {
        _session = session;
        _service = service;
        _navigate = navigate;
        _session.Commands.UndoRedoPerformed += OnUndoRedo;
    }

    public void Dispose() => _session.Commands.UndoRedoPerformed -= OnUndoRedo;

    public RangeObservableCollection<ClientSkillRowViewModel> Rows { get; } = new();

    [ObservableProperty] private bool _isLoading = true;
    [ObservableProperty] private ClientSkillEditorViewModel? _editor;
    [ObservableProperty] private ClientSkillRowViewModel? _selectedRow;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _statusText = string.Empty;

    public bool HasEditor => Editor is not null;
    partial void OnEditorChanged(ClientSkillEditorViewModel? value) => OnPropertyChanged(nameof(HasEditor));

    public bool CanDelete => SelectedRow is { IsCustom: true };

    partial void OnSelectedRowChanged(ClientSkillRowViewModel? value)
    {
        if (Editor is not null) Editor.PropertyChanged -= OnEditorPropertyChanged;
        Editor = value is null ? null : new ClientSkillEditorViewModel(value.Skill, _service, _session.Commands);
        if (Editor is not null) Editor.PropertyChanged += OnEditorPropertyChanged;
        OnPropertyChanged(nameof(CanDelete));
    }

    private void OnEditorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Keep the list's name column live as the skill name is edited.
        if (e.PropertyName == nameof(ClientSkillEditorViewModel.SkillName)) SelectedRow?.Refresh();
    }

    partial void OnSearchTextChanged(string value) => Filter();

    private void Filter()
    {
        string q = SearchText.Trim();
        var matched = q.Length == 0 ? _all : _all.Where(r => r.Matches(q)).ToList();
        Rows.ReplaceAll(matched);
        StatusText = matched.Count == _all.Count ? $"{_all.Count:N0} skills" : $"{matched.Count:N0} of {_all.Count:N0} skills";
    }

    private void OnUndoRedo()
    {
        SelectedRow?.Refresh();
        if (SelectedRow is { } row)
        {
            if (Editor is not null) Editor.PropertyChanged -= OnEditorPropertyChanged;
            Editor = new ClientSkillEditorViewModel(row.Skill, _service, _session.Commands);
            Editor.PropertyChanged += OnEditorPropertyChanged;
        }
    }

    [RelayCommand]
    private void AddCustom()
    {
        var dlg = new Views.IdInputDialog("New client skill", "SKID id", _service.NextFreeId())
        { Owner = System.Windows.Application.Current.MainWindow };
        if (dlg.ShowDialog() != true) return;

        var skill = _service.AllocateNew(dlg.Value);
        var row = new ClientSkillRowViewModel(skill, isCustom: true);
        _all.Add(row);
        Filter();
        SelectedRow = row;
    }

    [RelayCommand]
    private void DeleteEntry()
    {
        if (SelectedRow is not { IsCustom: true } row) return;
        if (!Views.ConfirmDialog.Show("Delete client skill",
                $"Delete the unsaved custom skill '{row.Constant}'?", yes: "Delete")) return;
        _service.Remove(row.Constant);
        _all.Remove(row);
        Filter();
        SelectedRow = Rows.FirstOrDefault();
    }

    [RelayCommand]
    private void CopyLua() => Editor?.CopyLuaCommand.Execute(null);

    /// <summary>Jumps to the server Skills section for the selected skill (matched by its id).</summary>
    [RelayCommand]
    private void SelectInSkills()
    {
        if (SelectedRow is { } row) _navigate?.Invoke("skill_db", RecordKey.Of(row.Id));
    }

    /// <summary>Selects a skill by constant (string key) or id (numeric key) — used by cross-navigation
    /// from the server Skills list. Deferred until the list has loaded.</summary>
    public void SelectRow(RecordKey key)
    {
        if (_all.Count == 0) { _pendingSelect = key; return; }
        var match = key.IsString
            ? _all.FirstOrDefault(r => string.Equals(r.Constant, key.AsString, StringComparison.Ordinal))
            : _all.FirstOrDefault(r => r.Id == (int)key.AsInt);
        if (match is null) return;
        if (SearchText.Length > 0) SearchText = string.Empty; // clear any filter so the row is visible
        SelectedRow = match;
    }

    public async Task EnsureLoadedAsync()
    {
        if (_all.Count > 0) return;
        IsLoading = true;
        try
        {
            var skills = await Task.Run(() => _service.ListSkills());
            _all = skills.Select(s => new ClientSkillRowViewModel(s, isCustom: false)).ToList();
        }
        catch (Exception ex)
        {
            IsLoading = false;
            Serilog.Log.Error(ex, "Failed to load client skills");
            Views.ConfirmDialog.Alert("Couldn't load Client Skills",
                $"Client Skills could not be loaded — a skill lua file may be malformed:\n\n{ex.Message}");
            return;
        }

        Filter();
        IsLoading = false;
        if (_pendingSelect is { } sel) { _pendingSelect = null; SelectRow(sel); }
        else SelectedRow = Rows.FirstOrDefault();
    }
}
