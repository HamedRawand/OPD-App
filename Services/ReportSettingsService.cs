using System.IO;
using System.Text.Json;
using OPDClinic.Models;

namespace OPDClinic.Services;

/// <summary>Loads and saves report design settings as JSON.
/// Singleton access via <see cref="Current"/>.</summary>
public static class ReportSettingsService
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "OPDClinic", "report_settings.json");

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    private static ReportSettings? _current;

    /// <summary>Returns the current (cached) settings, loading from disk on first access.</summary>
    public static ReportSettings Current => _current ??= Load();

    /// <summary>Saves the supplied settings to disk and updates the in-memory cache.</summary>
    public static void Save(ReportSettings settings)
    {
        _current = settings;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(settings, JsonOpts));
        }
        catch { /* swallow — don't crash the app if settings can't be persisted */ }
    }

    /// <summary>Forces a reload from disk (call after an external change).</summary>
    public static void Reload() => _current = null;

    private static ReportSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<ReportSettings>(
                           File.ReadAllText(FilePath)) ?? new ReportSettings();
        }
        catch { }
        return new ReportSettings();
    }
}
