using System;
using System.Text;

namespace MidgardStudio.Core.Serialization;

/// <summary>
/// Merges a freshly-regenerated db document into an existing import file's text, preserving the original's
/// comments (GPL banner, field docs, commented-out example entries) and replacing only the active top-level
/// <c>Body:</c> sequence. Used by <see cref="YamlDbWriter.WriteFile"/> so a save never wipes a hand-documented
/// import file (e.g. <c>import/mob_avail.yml</c>, which ships as a banner + commented examples with no active
/// entries — a plain regenerate would erase all of it).
/// </summary>
public static class YamlBodyMerge
{
    /// <summary>The text to write: the original's comments + Header preserved, with its active top-level Body
    /// replaced by (or, if it had none, the regenerated Body appended after) the canonical document's Body.
    /// Falls back to <paramref name="canonical"/> when the original is empty or isn't a recognizable rAthena db
    /// file (no top-level <c>Header:</c>) — so app-generated files (no banner, real Body) round-trip unchanged.</summary>
    public static string Merge(string? original, string canonical)
    {
        if (string.IsNullOrWhiteSpace(original)) return canonical;

        var lines = Normalize(original).Split('\n');
        if (IndexOfTopLevelKey(lines, "Header") < 0) return canonical; // not a db file we recognize

        var canonicalBody = ExtractBody(canonical);
        if (canonicalBody is null) return canonical;

        int bodyStart = IndexOfTopLevelKey(lines, "Body");
        if (bodyStart < 0)
        {
            // No active Body in the original (mob_avail ships a commented "#Body:"). Nothing to add → leave the
            // documented file untouched; otherwise append a real Body, keeping the banner + examples verbatim.
            if (IsEmptyBody(canonicalBody)) return original;
            return original.TrimEnd('\n', '\r') + "\n\n" + EnsureTrailingNewline(canonicalBody);
        }

        // Replace the original's Body block (from "Body:" to the next top-level key / EOF) with the new one.
        int bodyEnd = NextTopLevelKey(lines, bodyStart + 1);
        var sb = new StringBuilder();
        for (int i = 0; i < bodyStart; i++) sb.Append(lines[i]).Append('\n');
        sb.Append(EnsureTrailingNewline(canonicalBody));
        for (int i = bodyEnd; i < lines.Length; i++) sb.Append(lines[i]).Append('\n');
        return sb.ToString();
    }

    private static string Normalize(string text) => text.Replace("\r\n", "\n").Replace('\r', '\n');

    private static string EnsureTrailingNewline(string s) => s.EndsWith("\n", StringComparison.Ordinal) ? s : s + "\n";

    /// <summary>Index of the first line that is the top-level mapping key <paramref name="key"/> (column 0, not
    /// a comment, immediately followed by ':'), or -1. A commented "#Body:" is not matched.</summary>
    private static int IndexOfTopLevelKey(string[] lines, string key)
    {
        for (int i = 0; i < lines.Length; i++)
        {
            var l = lines[i];
            if (l.Length > key.Length && l[0] != ' ' && l[0] != '\t' && l[0] != '#'
                && l.StartsWith(key, StringComparison.Ordinal) && l[key.Length] == ':')
                return i;
        }
        return -1;
    }

    /// <summary>The next line (from <paramref name="from"/>) that begins a top-level key — a column-0 letter
    /// followed eventually by ':' — or EOF. Body entries (indented) and comments (#) are skipped, so this lands
    /// on a sibling key like <c>Footer:</c> if present, else EOF (Body is normally last).</summary>
    private static int NextTopLevelKey(string[] lines, int from)
    {
        for (int i = from; i < lines.Length; i++)
        {
            var l = lines[i];
            if (l.Length > 0 && char.IsLetter(l[0]) && l.Contains(':')) return i;
        }
        return lines.Length;
    }

    private static bool IsEmptyBody(string body)
    {
        var t = body.Trim();
        return t is "Body:" or "Body: []" or "Body: {}";
    }

    /// <summary>The Body region of a canonical Header+Body document — from its top-level <c>Body:</c> line to
    /// the end.</summary>
    private static string? ExtractBody(string canonical)
    {
        var lines = Normalize(canonical).Split('\n');
        int idx = IndexOfTopLevelKey(lines, "Body");
        if (idx < 0) return null;
        var sb = new StringBuilder();
        for (int i = idx; i < lines.Length; i++)
        {
            sb.Append(lines[i]);
            if (i < lines.Length - 1) sb.Append('\n');
        }
        return sb.ToString().TrimEnd('\n');
    }
}
