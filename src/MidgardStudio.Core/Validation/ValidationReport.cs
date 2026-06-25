namespace MidgardStudio.Core.Validation;

/// <summary>An immutable snapshot of validation findings with convenient roll-ups.</summary>
public sealed class ValidationReport
{
    public ValidationReport(IEnumerable<ValidationIssue> issues) => Issues = issues.ToList();

    public static ValidationReport Empty { get; } = new(Array.Empty<ValidationIssue>());

    public IReadOnlyList<ValidationIssue> Issues { get; }

    public int ErrorCount => Issues.Count(i => i.Severity == ValidationSeverity.Error);

    public int WarningCount => Issues.Count(i => i.Severity == ValidationSeverity.Warning);

    public int InfoCount => Issues.Count(i => i.Severity == ValidationSeverity.Info);

    public bool HasErrors => ErrorCount > 0;

    public IEnumerable<IGrouping<string, ValidationIssue>> ByDatabase => Issues.GroupBy(i => i.DbId);
}
