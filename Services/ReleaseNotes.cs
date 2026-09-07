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
        new("2.1.8", "2026-06-19",
        [
            "Dashboard: Create Backup quick action is now restricted to admin roles",
            "Options > Dosage: Type column now supports multiple selections (checklist popup)",
            "Prescription: Dosage dropdown now filters by the selected medicine form type",
        ]),
        new("2.1.7", "2026-06-19",
        [
            "Print button icons now display correctly on Windows 7",
        ]),
    ];

    public const string GitHubReleasesUrl = "https://github.com/HamedRawand/OPD-App/releases";
}
