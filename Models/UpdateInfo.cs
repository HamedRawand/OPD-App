namespace OPDClinic.Models;

/// <summary>Describes an available software update retrieved from GitHub Releases.</summary>
public record UpdateInfo(
    string Version,
    string ReleaseName,
    string ReleaseNotes,
    string DownloadUrl);
