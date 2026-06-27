using System;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace MidgardStudio.App.Services;

/// <summary>
/// Checks GitHub Releases for a newer published version on startup. Fail-silent by design — offline, a
/// rate-limited API, or an unexpected response simply means no update is shown. Anonymous (the public
/// releases API allows unauthenticated reads); pre-releases are ignored.
/// </summary>
public static class UpdateCheckService
{
    public const string ReleasesPage = "https://github.com/fahhadalsubaie/MidgardStudio/releases";
    private const string LatestApi = "https://api.github.com/repos/fahhadalsubaie/MidgardStudio/releases/latest";

    private static readonly HttpClient Http = CreateClient();

    private static HttpClient CreateClient()
    {
        var c = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
        c.DefaultRequestHeaders.UserAgent.ParseAdd("MidgardStudio-UpdateCheck"); // GitHub requires a User-Agent
        return c;
    }

    /// <summary>The latest release (version string + its page url) when it's newer than this build, else null.</summary>
    public static async Task<(string Version, string Url)?> CheckAsync()
    {
        try
        {
            string json = await Http.GetStringAsync(LatestApi).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("prerelease", out var pre) && pre.GetBoolean()) return null;

            string tag = root.TryGetProperty("tag_name", out var t) ? t.GetString() ?? string.Empty : string.Empty;
            string url = root.TryGetProperty("html_url", out var u) ? u.GetString() ?? ReleasesPage : ReleasesPage;

            var latest = Parse(tag);
            var current = Parse(Assembly.GetExecutingAssembly().GetName().Version?.ToString());
            if (latest is not null && current is not null && latest > current)
                return (tag.TrimStart('v', 'V'), url);
        }
        catch { /* offline / rate-limited / response shape changed — show nothing */ }
        return null;
    }

    private static Version? Parse(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        s = s.TrimStart('v', 'V').Trim();
        // Compare on major.minor.patch only (the assembly version carries a 4th, always-zero, field).
        return Version.TryParse(s, out var v) ? new Version(v.Major, v.Minor, Math.Max(0, v.Build)) : null;
    }
}
