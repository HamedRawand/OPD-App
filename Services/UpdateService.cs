using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using OPDClinic.Models;
using Serilog;

namespace OPDClinic.Services;

public static class UpdateService
{
    private const string ApiUrl =
        "https://api.github.com/repos/HamedRawand/OPD-App/releases/latest";

    private static readonly string CooldownFile =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OPDClinic", "last_update_check.txt");

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Calls the GitHub Releases API and returns an <see cref="UpdateInfo"/> when a
    /// version newer than the running assembly is available, otherwise null.
    /// Never throws — all errors are caught and logged.
    /// </summary>
    public static async Task<UpdateInfo?> CheckForUpdateAsync()
    {
        try
        {
            var current = GetCurrentVersion();
            if (current is null) return null;   // dev build (0.0.0.0) — skip

            using var client = BuildClient();
            var json = await client.GetStringAsync(ApiUrl);
            return ParseRelease(json, current);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Update check failed");
            return null;
        }
        finally
        {
            SaveCooldownTimestamp();
        }
    }

    /// <summary>
    /// Returns true if the 24-hour cooldown has NOT elapsed since the last check.
    /// Use to skip the automatic startup check.
    /// </summary>
    public static bool IsWithinCooldown()
    {
        try
        {
            if (!File.Exists(CooldownFile)) return false;
            var text = File.ReadAllText(CooldownFile).Trim();
            if (!DateTime.TryParse(text, out var last)) return false;
            return (DateTime.UtcNow - last).TotalHours < 24;
        }
        catch { return false; }
    }

    /// <summary>
    /// Downloads the installer exe from <paramref name="info"/>.DownloadUrl to %TEMP%,
    /// launches it with /SILENT /RESTARTAPPLICATIONS, then shuts down the current app.
    /// Reports download progress (0-100) via <paramref name="progress"/>.
    /// </summary>
    public static async Task DownloadAndInstallAsync(
        UpdateInfo info, IProgress<int>? progress = null)
    {
        var fileName = $"OPDClinic_Setup_v{info.Version}.exe";
        var tempPath = Path.Combine(Path.GetTempPath(), fileName);

        // ── Download ─────────────────────────────────────────────────────────
        using var client = BuildClient();
        var response = await client.GetAsync(
            info.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var total = response.Content.Headers.ContentLength ?? -1L;
        await using var stream = await response.Content.ReadAsStreamAsync();

        // Use a block scope so the file is fully closed before Process.Start tries to run it.
        await using (var file = File.Create(tempPath))
        {
            var buffer     = new byte[81_920];
            long downloaded = 0;
            int  read;
            while ((read = await stream.ReadAsync(buffer)) > 0)
            {
                await file.WriteAsync(buffer.AsMemory(0, read));
                downloaded += read;
                if (total > 0)
                    progress?.Report((int)(downloaded * 100L / total));
            }
        } // file stream closed & flushed here

        progress?.Report(100);
        Log.Information("Update downloaded to {Path}", tempPath);

        // ── Launch installer & exit ───────────────────────────────────────────
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName        = tempPath,
            Arguments       = "/SILENT /RESTARTAPPLICATIONS",
            UseShellExecute = true
        });

        Application.Current.Dispatcher.Invoke(Application.Current.Shutdown);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static HttpClient BuildClient()
    {
        var client = new HttpClient();
        // GitHub API requires a User-Agent header.
        client.DefaultRequestHeaders.Add("User-Agent", "OPDClinic-Updater/1.0");
        client.Timeout = TimeSpan.FromSeconds(30);
        return client;
    }

    /// <summary>Returns null for dev builds (version 0.0.0.0).</summary>
    private static Version? GetCurrentVersion()
    {
        var v = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0, 0);
        return v.Major == 0 && v.Minor == 0 ? null : v;
    }

    private static UpdateInfo? ParseRelease(string json, Version current)
    {
        using var doc  = JsonDocument.Parse(json);
        var root       = doc.RootElement;

        // Tag: "v1.0.1" → Version(1, 0, 1)
        var tag        = root.GetProperty("tag_name").GetString() ?? "";
        var versionStr = tag.TrimStart('v');
        if (!Version.TryParse(versionStr, out var latest)) return null;

        // Already up to date?
        if (latest <= current) return null;

        // Find first .exe asset
        string? downloadUrl = null;
        foreach (var asset in root.GetProperty("assets").EnumerateArray())
        {
            var name = asset.GetProperty("name").GetString() ?? "";
            if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                downloadUrl = asset.GetProperty("browser_download_url").GetString();
                break;
            }
        }

        if (downloadUrl is null)
        {
            Log.Warning("GitHub release {Tag} has no .exe asset — skipping update", tag);
            return null;
        }

        var releaseName = root.TryGetProperty("name", out var n)
            ? n.GetString() ?? tag : tag;
        var body = root.TryGetProperty("body", out var b)
            ? b.GetString() ?? "" : "";

        Log.Information("Update available: {Current} → {Latest}", current, latest);
        return new UpdateInfo(versionStr, releaseName, body, downloadUrl);
    }

    private static void SaveCooldownTimestamp()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(CooldownFile)!);
            File.WriteAllText(CooldownFile, DateTime.UtcNow.ToString("O"));
        }
        catch { /* non-critical */ }
    }
}
