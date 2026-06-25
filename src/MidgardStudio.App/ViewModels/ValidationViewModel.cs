using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MidgardStudio.App.Services;
using MidgardStudio.Core.Validation;

namespace MidgardStudio.App.ViewModels;

/// <summary>
/// Runs the workspace-wide validator (server correctness + cross-references + cross-file client/GRF
/// rules) on a background thread and presents the findings: filterable by severity, scoped to custom
/// entries or a full scan, with one-click quick-fixes and jump-to-record navigation.
/// </summary>
public sealed partial class ValidationViewModel : ObservableObject
{
    private readonly WorkspaceValidator _validator;
    private List<ValidationIssue> _all = new();

    public ValidationViewModel(WorkspaceValidator validator) => _validator = validator;

    public ObservableCollection<ValidationIssue> Issues { get; } = new();

    /// <summary>Set by the shell so "Go to" / double-click jumps to the offending record (dbId, key).</summary>
    public Action<string, string>? Navigate { get; set; }

    [ObservableProperty]
    private string _summary = "Run a check to validate your custom entries across the server and client files.";

    [ObservableProperty]
    private bool _isRunning;

    /// <summary>When true, validate every record (including the read-only base data), not just customs.</summary>
    [ObservableProperty]
    private bool _fullScan;

    [ObservableProperty]
    private bool _showErrors = true;

    [ObservableProperty]
    private bool _showWarnings = true;

    [ObservableProperty]
    private bool _showInfos = true;

    [ObservableProperty]
    private int _errorCount;

    [ObservableProperty]
    private int _warningCount;

    [ObservableProperty]
    private int _infoCount;

    [RelayCommand]
    private async Task Run()
    {
        if (IsRunning) return;
        IsRunning = true;
        Summary = "Validating…";
        try
        {
            var scope = FullScan ? ValidationScope.FullScan : ValidationScope.CustomOnly;
            var report = await Task.Run(() => _validator.Validate(scope));
            _all = report.Issues
                .OrderByDescending(i => i.Severity)
                .ThenBy(i => i.DbId, StringComparer.Ordinal)
                .ToList();
            ErrorCount = report.ErrorCount;
            WarningCount = report.WarningCount;
            InfoCount = report.InfoCount;
            ApplyFilter();
        }
        finally
        {
            IsRunning = false;
        }
    }

    partial void OnShowErrorsChanged(bool value) => ApplyFilter();
    partial void OnShowWarningsChanged(bool value) => ApplyFilter();
    partial void OnShowInfosChanged(bool value) => ApplyFilter();

    private void ApplyFilter()
    {
        Issues.Clear();
        foreach (var issue in _all)
        {
            bool show = issue.Severity switch
            {
                ValidationSeverity.Error => ShowErrors,
                ValidationSeverity.Warning => ShowWarnings,
                _ => ShowInfos,
            };
            if (show) Issues.Add(issue);
        }

        Summary = _all.Count == 0
            ? "No issues found in your custom/overridden entries."
            : $"{ErrorCount} error(s), {WarningCount} warning(s), {InfoCount} info — showing {Issues.Count}.";
    }

    [RelayCommand]
    private async Task ApplyFix(ValidationIssue? issue)
    {
        if (issue?.Fix is null) return;
        issue.Fix.Apply();
        await RunCommand.ExecuteAsync(null); // re-validate so the list reflects the fix
    }

    [RelayCommand]
    private void Open(ValidationIssue? issue)
    {
        if (issue is not null) Navigate?.Invoke(issue.DbId, issue.Key);
    }
}
