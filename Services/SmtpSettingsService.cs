using System.IO;
using System.Text.Json;
using OPDClinic.Models;

namespace OPDClinic.Services;

/// <summary>Loads and saves SMTP settings to/from a JSON file.</summary>
public static class SmtpSettingsService
{
    private static readonly string _path = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "OPDClinic", "smtp_settings.json");

    private static SmtpSettings? _current;

    public static SmtpSettings Current
    {
        get
        {
            if (_current is not null) return _current;
            Reload();
            return _current!;
        }
    }

    public static void Reload()
    {
        try
        {
            if (File.Exists(_path))
            {
                var json = File.ReadAllText(_path);
                _current = JsonSerializer.Deserialize<SmtpSettings>(json) ?? new SmtpSettings();
            }
            else
            {
                _current = new SmtpSettings();
            }
        }
        catch
        {
            _current = new SmtpSettings();
        }
    }

    public static void Save(SmtpSettings settings)
    {
        _current = settings;
        var dir = Path.GetDirectoryName(_path)!;
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_path, json);
    }
}
