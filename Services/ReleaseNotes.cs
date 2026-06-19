namespace OPDClinic.Services;

/// <summary>
/// Bundled release history shown in the About dialog.
/// Update this alongside the version bump in OPDClinic.csproj and installer.iss.
/// Keep the two most recent entries — older history lives in the GitHub releases page.
/// </summary>
public static class ReleaseNotes
{
    public sealed record Entry(string Version, string Date, string[] Items);

    public static readonly Entry[] Recent =
    [
        new("2.1.6", "2026-06-19",
        [
            "App icons now display correctly on Windows 7 (sidebar, title bar, taskbar)",
        ]),
        new("2.1.5", "2026-06-19",
        [
            "Windows 7 SP1 and above now supported (32-bit and 64-bit)",
        ]),
    ];

    public const string GitHubReleasesUrl = "https://github.com/HamedRawand/OPD-App/releases";
}
