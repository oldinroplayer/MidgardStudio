namespace MidgardStudio.Core;

/// <summary>
/// Central, Windows-standard locations for the files Midgard Studio creates. Small user config lives in
/// Roaming AppData (may sync across machines); logs are machine-local in Local AppData; backups live under
/// the user's Documents so they're easy to find and copy. All use the "Midgard Studio" app folder.
/// </summary>
public static class AppPaths
{
    private const string AppFolder = "Midgard Studio";

    /// <summary>%APPDATA%\Midgard Studio — settings + workspace profiles (small; may roam).</summary>
    public static string RoamingDir { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppFolder);

    /// <summary>%LOCALAPPDATA%\Midgard Studio — logs and other machine-local, disposable data.</summary>
    public static string LocalDir { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppFolder);

    /// <summary>Documents\Midgard Studio\Backups — user-visible save snapshots.</summary>
    public static string BackupsDir { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), AppFolder, "Backups");

    /// <summary>The pre-1.0 backups location (%APPDATA%\Midgard Studio\backups), kept only so a one-time
    /// migration can move old snapshots into <see cref="BackupsDir"/>.</summary>
    public static string LegacyRoamingBackupsDir { get; } = Path.Combine(RoamingDir, "backups");
}
