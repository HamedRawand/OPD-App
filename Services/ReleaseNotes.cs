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
        new("2.1.9", "2026-09-08",
        [
            "New: Prescription Templates — save a common case's medicines, lab tests, and notes as a reusable preset, then apply it to a new visit in one click",
            "Forgot Password email now works out of the box on a fresh install — no manual Email Settings step required",
        ]),
        new("2.1.8", "2026-06-19",
        [
            "Dashboard: Create Backup quick action is now restricted to admin roles",
            "Options > Dosage: Type column now supports multiple selections (checklist popup)",
            "Prescription: Dosage dropdown now filters by the selected medicine form type",
        ]),
    ];

    public const string GitHubReleasesUrl = "https://github.com/HamedRawand/OPD-App/releases";
}
