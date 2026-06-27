using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using MidgardStudio.Core.Workspace;

namespace MidgardStudio.App.ViewModels;

/// <summary>
/// First-run / profile-management screen. Lets the user point the app at a server's files (server DB,
/// client lua-files, SystemEN) and client GRF archives, pick the Renewal/Pre-Renewal system, and save
/// the whole thing as a named profile so multiple servers can be managed. Clicking a recent profile
/// loads its values; "Open" raises <see cref="OpenRequested"/> for the shell to apply.
/// </summary>
public sealed partial class ConfigurationWizardViewModel : ObservableObject
{
    private readonly IWorkspaceConfigService _config;

    public ConfigurationWizardViewModel(IWorkspaceConfigService config)
    {
        _config = config;
        Refresh();
    }

    /// <summary>Raised when the user clicks "Open" — carries the profile the shell should apply.</summary>
    public event Action<WorkspaceConfig>? OpenRequested;

    /// <summary>Raised when the user closes the configuration window. The shell dismisses the overlay if a
    /// valid workspace is already loaded behind it, or exits the app on a genuine first run.</summary>
    public event Action? CloseRequested;

    /// <summary>Saved profiles, most-recently-opened first.</summary>
    public ObservableCollection<WorkspaceConfig> Profiles { get; } = new();

    /// <summary>GRF archives and/or loose data folders for this profile (layered, last wins).</summary>
    public ObservableCollection<string> GrfPaths { get; } = new();

    [ObservableProperty] private WorkspaceConfig? _selectedProfile;
    [ObservableProperty] private string _profileName = "Default";
    [ObservableProperty] private string _serverDbRoot = string.Empty;
    [ObservableProperty] private string _luaFilesRoot = string.Empty;
    [ObservableProperty] private string _itemInfoPath = string.Empty;
    [ObservableProperty] private string _itemInfoCustomPath = string.Empty;
    [ObservableProperty] private bool _isRenewal = true;
    [ObservableProperty] private int _selectedCodepage = 1252;
    [ObservableProperty] private string _statusMessage = string.Empty;

    /// <summary>Display Encoding choices (codepage + friendly label). Drives how the app decodes text for
    /// viewing and how it reads/writes the loose client lua files; server YAML is always saved as UTF-8.</summary>
    public IReadOnlyList<EncodingOption> EncodingOptions { get; } = new[]
    {
        new EncodingOption("Western — Windows-1252 (Latin)", 1252),
        new EncodingOption("Korean — EUC-KR (949)", 949),
        new EncodingOption("Cyrillic — Windows-1251", 1251),
        new EncodingOption("Japanese — Shift-JIS (932)", 932),
        new EncodingOption("Simplified Chinese — GBK (936)", 936),
        new EncodingOption("Thai — Windows-874", 874),
        new EncodingOption("Traditional Chinese — Big5 (950)", 950),
    };

    /// <summary>Reloads the profile list and the editable fields (active profile, or repo defaults).</summary>
    public void Refresh()
    {
        RefreshProfiles();
        var active = _config.ActiveProfile;
        LoadConfig(active ?? WorkspaceConfigService.CreateDefault());
        StatusMessage = active is null
            ? "First run — confirm these paths (defaults shown), choose a system, then Open."
            : string.Empty;
    }

    [RelayCommand]
    private void Close() => CloseRequested?.Invoke();

    private void RefreshProfiles()
    {
        Profiles.Clear();
        foreach (var p in _config.GetProfiles()) Profiles.Add(p);
    }

    private void LoadConfig(WorkspaceConfig cfg)
    {
        ProfileName = cfg.Name;
        ServerDbRoot = cfg.Paths.ServerDbRoot;
        LuaFilesRoot = cfg.Paths.LuaFilesRoot;
        ItemInfoPath = cfg.Paths.ItemInfoPath;
        ItemInfoCustomPath = cfg.Paths.ItemInfoCustomPath;
        IsRenewal = cfg.DefaultMode == ServerMode.Renewal;
        SelectedCodepage = cfg.ClientCodepage > 0 ? cfg.ClientCodepage : 1252;
        GrfPaths.Clear();
        foreach (var g in cfg.GrfPaths) GrfPaths.Add(g);
    }

    partial void OnSelectedProfileChanged(WorkspaceConfig? value)
    {
        if (value is null) return;
        LoadConfig(value);
        StatusMessage = $"Loaded profile '{value.Name}'.";
    }

    [RelayCommand]
    private void NewProfile()
    {
        SelectedProfile = null;
        LoadConfig(WorkspaceConfigService.CreateDefault());
        ProfileName = UniqueName("New Server");
        StatusMessage = "New profile — set its paths, then Save or Open.";
    }

    [RelayCommand]
    private void DeleteProfile(WorkspaceConfig? profile)
    {
        profile ??= SelectedProfile;
        if (profile is null) return;
        _config.DeleteProfile(profile.Name);
        RefreshProfiles();
        StatusMessage = $"Deleted profile '{profile.Name}'.";
    }

    [RelayCommand] private void BrowseServerDb() { if (PickFolder(ServerDbRoot) is { } d) ServerDbRoot = d; }
    [RelayCommand] private void BrowseLuaFiles() { if (PickFolder(LuaFilesRoot) is { } d) LuaFilesRoot = d; }
    [RelayCommand] private void BrowseItemInfo() { if (PickLuaFile(ItemInfoPath) is { } f) ItemInfoPath = f; }
    [RelayCommand] private void BrowseItemInfoCustom() { if (PickLuaFile(ItemInfoCustomPath) is { } f) ItemInfoCustomPath = f; }

    [RelayCommand]
    private void AddGrf()
    {
        var dlg = new OpenFileDialog
        {
            Title = "Add GRF archive(s)",
            Filter = "GRF archives (*.grf;*.gpf)|*.grf;*.gpf|All files (*.*)|*.*",
            Multiselect = true,
        };
        if (dlg.ShowDialog() == true)
            foreach (var f in dlg.FileNames)
                if (!GrfPaths.Contains(f)) GrfPaths.Add(f);
    }

    [RelayCommand]
    private void AddDataFolder()
    {
        if (PickFolder(null) is { } d && !GrfPaths.Contains(d)) GrfPaths.Add(d);
    }

    [RelayCommand]
    private void RemoveGrf(string? path)
    {
        if (path is not null) GrfPaths.Remove(path);
    }

    /// <summary>Reorders a GRF/data path (drag-to-reorder). Layering is order-sensitive — the bottom entry wins.</summary>
    public void MoveGrf(int from, int to)
    {
        if (from < 0 || from >= GrfPaths.Count) return;
        to = Math.Clamp(to, 0, GrfPaths.Count - 1);
        if (from != to) GrfPaths.Move(from, to);
    }

    [RelayCommand]
    private void SaveProfile()
    {
        var cfg = BuildConfig();
        _config.UpsertProfile(cfg);
        RefreshProfiles();
        StatusMessage = $"Saved profile '{cfg.Name}'.";
    }

    [RelayCommand]
    private void Open()
    {
        var cfg = BuildConfig();
        if (string.IsNullOrWhiteSpace(cfg.Paths.ServerDbRoot) || !Directory.Exists(cfg.Paths.ServerDbRoot))
        {
            StatusMessage = "Server DB folder not found — pick the folder that contains 're' and 'import' (…\\server-db\\db).";
            return;
        }

        OpenRequested?.Invoke(cfg);
    }

    private WorkspaceConfig BuildConfig() => new()
    {
        Name = string.IsNullOrWhiteSpace(ProfileName) ? "Default" : ProfileName.Trim(),
        Paths = new WorkspacePaths
        {
            ServerDbRoot = ServerDbRoot.Trim(),
            LuaFilesRoot = LuaFilesRoot.Trim(),
            ItemInfoPath = ItemInfoPath.Trim(),
            ItemInfoCustomPath = ItemInfoCustomPath.Trim(),
        },
        DefaultMode = IsRenewal ? ServerMode.Renewal : ServerMode.PreRenewal,
        ClientCodepage = SelectedCodepage > 0 ? SelectedCodepage : 1252,
        GrfPaths = GrfPaths.ToList(),
    };

    private string UniqueName(string baseName)
    {
        string name = baseName;
        int n = 2;
        while (Profiles.Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)))
            name = $"{baseName} {n++}";
        return name;
    }

    private static string? PickFolder(string? initial)
    {
        var dlg = new OpenFolderDialog { Title = "Select folder" };
        if (!string.IsNullOrWhiteSpace(initial) && Directory.Exists(initial))
            dlg.InitialDirectory = initial;
        return dlg.ShowDialog() == true ? dlg.FolderName : null;
    }

    private static string? PickLuaFile(string? initial)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Select itemInfo file",
            Filter = "Lua item info (*.lua;*.lub)|*.lua;*.lub|All files (*.*)|*.*",
        };
        if (!string.IsNullOrWhiteSpace(initial))
        {
            var dir = Path.GetDirectoryName(initial);
            if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir)) dlg.InitialDirectory = dir;
            if (File.Exists(initial)) dlg.FileName = Path.GetFileName(initial);
        }
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }
}

/// <summary>A selectable Display Encoding (codepage + friendly label) for the configuration wizard combo.</summary>
public sealed record EncodingOption(string Label, int Codepage);
