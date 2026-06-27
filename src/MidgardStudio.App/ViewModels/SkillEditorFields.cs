using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MidgardStudio.App.ViewModels;

/// <summary>One level's value in a per-level field editor (e.g. "Lv 3 → 15 SP").</summary>
public sealed partial class LevelValueViewModel : ObservableObject
{
    private readonly Action _changed;
    public LevelValueViewModel(int level, int value, Action changed) { Level = level; _value = value; _changed = changed; }
    public int Level { get; }
    [ObservableProperty] private int _value;
    partial void OnValueChanged(int value) => _changed();
}

/// <summary>
/// A per-level integer array edited as a strip of small boxes — one per skill level — instead of a
/// hand-typed list. An enable toggle omits the field entirely (so passives don't get a spurious
/// <c>SpAmount = {0,...}</c>); "Fill" copies level 1 to every level. Auto-resizes to the skill's MaxLv.
/// </summary>
public sealed partial class PerLevelFieldViewModel : ObservableObject
{
    private readonly Action _changed;
    private bool _ready;
    private bool _suspend;
    private int _maxLv;

    public PerLevelFieldViewModel(string label, string hint, IReadOnlyList<int> initial, int maxLv, Action changed)
    {
        Label = label;
        Hint = hint;
        _changed = changed;
        _maxLv = Math.Max(1, maxLv);
        _enabled = initial.Count > 0;
        Build(initial);
        _ready = true;
    }

    public string Label { get; }
    public string Hint { get; }
    public ObservableCollection<LevelValueViewModel> Cells { get; } = new();

    [ObservableProperty] private bool _enabled;

    /// <summary>The current values (empty when the field is disabled → omitted on save).</summary>
    public List<int> Snapshot() => Enabled ? Cells.Select(c => c.Value).ToList() : new List<int>();

    /// <summary>Rebuilds to a new MaxLv, preserving existing values (pads with the last). Programmatic —
    /// does not raise a change (the caller commits the whole resize in one batch).</summary>
    public void SetMaxLv(int maxLv) { _maxLv = Math.Max(1, maxLv); Build(Cells.Select(c => c.Value).ToList()); }

    private void Build(IReadOnlyList<int> values)
    {
        _suspend = true;
        Cells.Clear();
        if (Enabled)
        {
            int last = values.Count > 0 ? values[^1] : 0;
            for (int lv = 1; lv <= _maxLv; lv++)
                Cells.Add(new LevelValueViewModel(lv, lv - 1 < values.Count ? values[lv - 1] : last, OnCell));
        }
        _suspend = false;
    }

    private void OnCell() { if (_ready && !_suspend) _changed(); }

    partial void OnEnabledChanged(bool value)
    {
        if (!_ready) return;
        Build(Cells.Select(c => c.Value).ToList());
        _changed();
    }

    [RelayCommand]
    private void Fill()
    {
        if (Cells.Count == 0) return;
        _suspend = true;
        int v = Cells[0].Value;
        foreach (var c in Cells) c.Value = v;
        _suspend = false;
        _changed();
    }
}

/// <summary>One prerequisite row (a chosen skill + the level required), removable.</summary>
public sealed partial class PrereqRowViewModel : ObservableObject
{
    private readonly Action _changed;
    private readonly Action<PrereqRowViewModel> _remove;

    public PrereqRowViewModel(string skid, string display, int level, Action changed, Action<PrereqRowViewModel> remove)
    {
        Skid = skid;
        Display = display;
        _level = level < 1 ? 1 : level;
        _changed = changed;
        _remove = remove;
    }

    public string Skid { get; }
    public string Display { get; }
    [ObservableProperty] private int _level;
    partial void OnLevelChanged(int value) => _changed();

    [RelayCommand] private void Remove() => _remove(this);
}
