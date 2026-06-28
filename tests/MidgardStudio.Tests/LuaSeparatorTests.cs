using MidgardStudio.Core.Lua;

namespace MidgardStudio.Tests;

/// <summary>
/// The shared table-append separator rule (<see cref="LuaScan.SeparatorBeforeNewEntry"/>) tested in
/// isolation on strings — the v1.0.1 corruption (a missing comma before a new SKID entry) lived here,
/// previously only caught by full real-file round-trips.
/// </summary>
public class LuaSeparatorTests
{
    private static string Sep(string text)
    {
        int open = LuaScan.FindTableOpen(text, "T");
        int close = LuaScan.FindMatchingBrace(text, open);
        return LuaScan.SeparatorBeforeNewEntry(text, open, close);
    }

    [Fact] public void Empty_table_needs_no_separator() => Assert.Equal("", Sep("T = { }"));

    [Fact] public void Trailing_comma_needs_no_separator() => Assert.Equal("", Sep("T = {\n\ta = 1,\n}"));

    [Fact] public void Missing_comma_needs_a_separator() => Assert.Equal(",", Sep("T = {\n\ta = 1\n}"));

    [Fact] public void Trailing_semicolon_needs_no_separator() => Assert.Equal("", Sep("T = {\n\ta = 1;\n}"));
}
