namespace MidgardStudio.Core.Lookup;

/// <summary>
/// Fast existence check for cross-database references. A reference field stores the target record's
/// identifying name (an item/mob AegisName, a skill Aegis name, …); the validator asks whether that
/// name resolves to a real record. The App supplies a cached implementation over the loaded
/// workspace; tests supply an in-memory one.
/// </summary>
public interface IReferenceIndex
{
    /// <summary>True when this index has data loaded for <paramref name="dbId"/>. When a target db is
    /// not loaded, reference checks are skipped rather than reported as broken (avoids false positives).</summary>
    bool Knows(string dbId);

    /// <summary>True when <paramref name="referenceValue"/> resolves to an existing record in
    /// <paramref name="dbId"/> (case-insensitive, matching rAthena/GRF conventions).</summary>
    bool Contains(string dbId, string referenceValue);
}
