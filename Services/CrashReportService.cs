using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Serilog;

namespace OPDClinic.Services;

/// <summary>
/// Writes structured crash reports to the Logs directory and can open it in Explorer.
/// All methods are non-throwing — crash reporting must never crash the app.
/// </summary>
public static class CrashReportService
{
    public static string LogDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "OPDClinic", "Logs");

    /// <summary>
    /// Writes a crash report file and returns its full path (or the log directory
    /// on IO failure so the user still gets a useful location to look at).
    /// </summary>
    public static string WriteCrashReport(Exception ex, string context = "UI thread")
    {
        try
        {
            Directory.CreateDirectory(LogDirectory);

            var fileName = $"crash_{DateTime.Now:yyyyMMdd_HHmmss}.log";
            var path     = Path.Combine(LogDirectory, fileName);

            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "unknown";
            var user    = App.Auth?.CurrentUser?.Username ?? "(not logged in)";

            // Build inner-exception chain
            var innerSection = "";
            var inner = ex.InnerException;
            var depth = 0;
            while (inner is not null && depth++ < 5)
            {
                innerSection += $"\nInner Exception ({depth}):\n" +
                                $"  Type    : {inner.GetType().FullName}\n" +
                                $"  Message : {inner.Message}\n" +
                                $"  Stack   :\n{inner.StackTrace}\n";
                inner = inner.InnerException;
            }

            var report =
                $"Rx Writer — Crash Report\n" +
                $"========================\n" +
                $"Date/Time  : {DateTime.Now:yyyy-MM-dd HH:mm:ss} local  /  {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\n" +
                $"Version    : {version}\n" +
                $"Context    : {context}\n" +
                $"OS         : {RuntimeInformation.OSDescription}\n" +
                $"Architecture: {RuntimeInformation.ProcessArchitecture}\n" +
                $"User       : {user}\n" +
                $"\n" +
                $"Exception  : {ex.GetType().FullName}\n" +
                $"Message    : {ex.Message}\n" +
                $"\nStack Trace:\n{ex.StackTrace}\n" +
                innerSection;

            File.WriteAllText(path, report, System.Text.Encoding.UTF8);
            Log.Error(ex, "Crash report written to {Path}", path);
            return path;
        }
        catch (Exception writeEx)
        {
            Log.Error(writeEx, "Failed to write crash report");
            return LogDirectory;
        }
    }

    /// <summary>Opens the Logs folder in Windows Explorer (fire-and-forget).</summary>
    public static void OpenLogFolder()
    {
        try
        {
            Directory.CreateDirectory(LogDirectory);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName        = LogDirectory,
                UseShellExecute = true
            });
        }
        catch { /* non-critical */ }
    }
}
