using MidgardStudio.Core.Model;
using MidgardStudio.Core.Overlay;
using MidgardStudio.Core.Validation.Validators;

namespace MidgardStudio.Core.Validation;

/// <summary>
/// The headless orchestrator. Holds the registered record-level and overlay-level validators and
/// runs them over a single record (live editing) or a whole overlay (panel / on-save scan).
/// Pure Core — no UI, GRF, or client knowledge. App-level cross-file rules run separately.
/// </summary>
public sealed class ValidationEngine
{
    private readonly IReadOnlyList<IRecordValidator> _recordValidators;
    private readonly IReadOnlyList<IOverlayValidator> _overlayValidators;

    public ValidationEngine(IEnumerable<IRecordValidator> recordValidators, IEnumerable<IOverlayValidator> overlayValidators)
    {
        _recordValidators = recordValidators.ToList();
        _overlayValidators = overlayValidators.ToList();
    }

    /// <summary>The standard rule set: the generic schema-driven validator plus the bespoke per-db
    /// validators that encode logic the schema metadata can't express.</summary>
    public static ValidationEngine CreateDefault() => new(
        new IRecordValidator[]
        {
            new SchemaDrivenValidator(),
            new ItemDbValidator(),
            new MobDbValidator(),
            new MobAvailValidator(),
            new ItemComboValidator(),
            new ScriptSanityValidator(),
        },
        new IOverlayValidator[]
        {
            new UniqueFieldValidator(),
        });

    /// <summary>Validates one record (used live as the user edits). Does not run cross-record rules.</summary>
    public IEnumerable<ValidationIssue> ValidateRecord(DbRecord record, OverlayTable table, ValidationContext context)
    {
        string dbId = table.Schema.Id;
        foreach (var validator in _recordValidators)
        {
            if (!validator.AppliesTo(dbId)) continue;
            foreach (var issue in validator.Validate(record, table, context))
                yield return issue;
        }
    }

    /// <summary>Validates an overlay: every in-scope record plus the overlay-level (cross-record) rules.</summary>
    public IEnumerable<ValidationIssue> ValidateOverlay(OverlayTable table, ValidationScope scope, ValidationContext context)
    {
        foreach (var record in table.Effective())
        {
            if (scope == ValidationScope.CustomOnly && record.Origin == RecordOrigin.Base)
                continue;
            foreach (var issue in ValidateRecord(record, table, context))
                yield return issue;
        }

        string dbId = table.Schema.Id;
        foreach (var validator in _overlayValidators)
        {
            if (!validator.AppliesTo(dbId)) continue;
            foreach (var issue in validator.Validate(table, scope, context))
                yield return issue;
        }
    }
}
