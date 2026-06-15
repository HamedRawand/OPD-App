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
        new("2.1.3", "2026-06-15",
        [
            "Doctor built-in role: added medicine catalog management, delete patient/visit permissions",
            "About dialog: removed external GitHub release history link",
            "User manual fully updated to reflect all v2.1.x features",
        ]),
        new("2.1.2", "2026-06-15",
        [
            "Patient bar rearranged in RTL order: Name | Age | Sex | Date",
            "Patient ID row is now separate with show/hide toggle (نمبر مسلسل)",
            "Adjustable gap between header divider and patient bar",
            "Footer divider line now has its own independent settings",
            "Divider thickness sliders now step by 0.5 pt",
            "Fixed crash when using Dashed or Dotted divider style",
            "Custom role users now correctly see only their own patients",
            "Physician link field now available for custom role users",
            "Custom Role dialog fixed — buttons no longer get clipped on small screens",
        ]),
    ];

    public const string GitHubReleasesUrl = "https://github.com/HamedRawand/OPD-App/releases";
}
