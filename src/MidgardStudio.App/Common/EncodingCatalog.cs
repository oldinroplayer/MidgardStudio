namespace MidgardStudio.App.Common;

/// <summary>
/// The single source of truth for the Display Encoding choices — shared by the configuration wizard's
/// encoding dropdown and the backup encoding stamp, so their labels always match. (codepage, friendly label.)
/// </summary>
public static class EncodingCatalog
{
    public static IReadOnlyList<(string Label, int Codepage)> Choices { get; } = new[]
    {
        ("Western — Windows-1252 (Latin)", 1252),
        ("Korean — EUC-KR (949)", 949),
        ("Cyrillic — Windows-1251", 1251),
        ("Japanese — Shift-JIS (932)", 932),
        ("Simplified Chinese — GBK (936)", 936),
        ("Thai — Windows-874", 874),
        ("Traditional Chinese — Big5 (950)", 950),
    };

    /// <summary>The friendly label for a codepage (matching the profile dropdown), or a sensible fallback.</summary>
    public static string Label(int codepage)
    {
        foreach (var (label, cp) in Choices)
            if (cp == codepage) return label;
        return codepage <= 0 ? "Unknown" : "Codepage " + codepage;
    }
}
