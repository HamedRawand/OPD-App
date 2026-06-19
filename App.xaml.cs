using System.Windows;
using System.Windows.Media.Imaging;
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
    public static IDbContextFactory<AppDbContext> DbFactory { get; private set; } = null!;
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
            var ex = args.ExceptionObject as Exception;
            Log.Fatal(ex, "Fatal unhandled exception (CLR). IsTerminating={IsTerminating}",
                args.IsTerminating);
            if (ex is not null)
                Services.CrashReportService.WriteCrashReport(ex, "CLR non-UI thread");
            Log.CloseAndFlush();
        };

        Log.Information("Rx Writer starting up");

        // Explicitly set the window icon on every window via a class handler so it
        // shows correctly in the title bar and taskbar on Windows 7 SP1+.
        // Without this, WPF on Win7 sometimes fails to pick up the ApplicationIcon resource.
        var iconUri = new Uri("pack://application:,,,/OPDClinic;component/image/Caduceus.ico");
        EventManager.RegisterClassHandler(typeof(Window), Window.LoadedEvent,
            new RoutedEventHandler((s, _) =>
            {
                if (s is Window w && w.Icon == null)
                    w.Icon = BitmapFrame.Create(iconUri);
            }));

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

        DbFactory = new SimpleDbContextFactory(options);

        // Short-lived context for one-time startup operations
        using (var startupDb = DbFactory.CreateDbContext())
        {
            startupDb.Database.Migrate();
            DbSeeder.Seed(startupDb);

            // R12 migration: rename CreateEditPatient → RegisterPatient + EnterClinicalData
            // in any existing CustomRole.PermissionsJson rows
            var legacyRoles = startupDb.CustomRoles
                .Where(r => r.PermissionsJson != null && r.PermissionsJson.Contains("CreateEditPatient"))
                .ToList();
            foreach (var role in legacyRoles)
                role.PermissionsJson = role.PermissionsJson!
                    .Replace("CreateEditPatient", "RegisterPatient,EnterClinicalData");
            if (legacyRoles.Count > 0) startupDb.SaveChanges();
        }

        Auth = new AuthService(DbFactory);

        Log.Information("Database ready at {DbPath}", dbPath);

        var login = new Views.LoginWindow();
        login.Show();
    }

    // ── UI-thread unhandled exception ─────────────────────────────────────────
    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error(e.Exception, "Unhandled UI-thread exception");

        var crashPath = Services.CrashReportService.WriteCrashReport(e.Exception, "UI thread");
        Log.CloseAndFlush();

        var open = MessageBox.Show(
            $"An unexpected error occurred:\n\n" +
            $"{e.Exception.GetType().Name}: {e.Exception.Message}\n\n" +
            $"A crash report was saved to:\n{crashPath}\n\n" +
            "Open the log folder now?",
            "Unexpected Error",
            MessageBoxButton.YesNo,
            MessageBoxImage.Error);

        if (open == MessageBoxResult.Yes)
            Services.CrashReportService.OpenLogFolder();

        e.Handled = true; // prevent crash — keep app running
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("Rx Writer shutting down");
        Log.CloseAndFlush();
        base.OnExit(e);
    }

    /// <summary>Simple per-call factory — creates a fresh <see cref="AppDbContext"/> on every <c>CreateDbContext()</c>.</summary>
    private sealed class SimpleDbContextFactory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);
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
