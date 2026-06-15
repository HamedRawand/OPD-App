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
        new("2.1.4", "2026-06-16",
        [
            "Patient ID resets to P-00001 on fresh install or after all patients are deleted",
        ]),
        new("2.1.3", "2026-06-15",
        [
            "Doctor built-in role: added medicine catalog management, delete patient/visit permissions",
            "About dialog: removed external GitHub release history link",
            "User manual fully updated to reflect all v2.1.x features",
        ]),
    ];

    public const string GitHubReleasesUrl = "https://github.com/HamedRawand/OPD-App/releases";
}
