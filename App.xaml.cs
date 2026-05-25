using System.Windows;
using System.Windows.Threading;
using Microsoft.EntityFrameworkCore;
using OPDClinic.Data;
using OPDClinic.Services;
using QuestPDF.Infrastructure;
using Serilog;
using System.IO;

namespace OPDClinic;

public partial class App : Application
{
    public static AppDbContext Db { get; private set; } = null!;
    public static AuthService Auth { get; private set; } = null!;
    public static string DbPath { get; private set; } = "";

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        // ── Logging ────────────────────────────────────────────────────────────
        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OPDClinic", "Logs");
        Directory.CreateDirectory(logDir);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                path: Path.Combine(logDir, "opdclinic_.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        // ── Global unhandled-exception hook (non-UI thread) ───────────────────
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            Log.Fatal(args.ExceptionObject as Exception,
                "Fatal unhandled exception (CLR). IsTerminating={IsTerminating}",
                args.IsTerminating);
            Log.CloseAndFlush();
        };

        Log.Information("OPD Clinic starting up");

        // ── PDF licence ────────────────────────────────────────────────────────
        QuestPDF.Settings.License = LicenseType.Community;

        LanguageService.Initialize();
        CleanupTempPdfs();

        // ── Database ──────────────────────────────────────────────────────────
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OPDClinic", "clinic.db");
        DbPath = dbPath;
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;

        Db = new AppDbContext(options);
        Db.Database.Migrate();
        DbSeeder.Seed(Db);

        Auth = new AuthService(Db);

        Log.Information("Database ready at {DbPath}", dbPath);

        var login = new Views.LoginWindow();
        login.Show();
    }

    // ── UI-thread unhandled exception ─────────────────────────────────────────
    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error(e.Exception, "Unhandled UI-thread exception");
        Log.CloseAndFlush();

        MessageBox.Show(
            $"An unexpected error occurred:\n\n{e.Exception.Message}\n\nDetails have been saved to the log file.",
            "Unexpected Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);

        e.Handled = true; // prevent crash — keep app running
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("OPD Clinic shutting down");
        Log.CloseAndFlush();
        base.OnExit(e);
    }

    private static void CleanupTempPdfs()
    {
        try
        {
            var cutoff = DateTime.UtcNow.AddDays(-1);
            foreach (var file in Directory.GetFiles(Path.GetTempPath(), "Rx_*.pdf"))
            {
                if (File.GetLastWriteTime(file).ToUniversalTime() < cutoff)
                    File.Delete(file);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Temp PDF cleanup failed (non-fatal)");
        }
    }
}
