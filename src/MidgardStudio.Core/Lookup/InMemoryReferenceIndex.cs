namespace MidgardStudio.Core.Lookup;

/// <summary>
/// A simple in-memory <see cref="IReferenceIndex"/> backed by per-database name sets. Useful for
/// headless tests and as a building block; the App's live index is built from the loaded overlays.
/// </summary>
public sealed class InMemoryReferenceIndex : IReferenceIndex
{
    private readonly Dictionary<string, HashSet<string>> _byDb = new(StringComparer.Ordinal);

    /// <summary>Registers (or extends) the set of valid reference names for a database. Calling this
    /// for a db — even with no names — marks the db as "known".</summary>
    public InMemoryReferenceIndex Add(string dbId, params string[] names)
    {
        if (!_byDb.TryGetValue(dbId, out var set))
            _byDb[dbId] = set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in names)
            if (!string.IsNullOrWhiteSpace(name))
                set.Add(name);
        return this;
    }

    public bool Knows(string dbId) => _byDb.ContainsKey(dbId);

    public bool Contains(string dbId, string referenceValue) =>
        _byDb.TryGetValue(dbId, out var set) && set.Contains(referenceValue);
}
