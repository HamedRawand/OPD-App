using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OPDClinic.Services;

namespace OPDClinic.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    // ── Stat cards ────────────────────────────────────────────────────────────
    [ObservableProperty] private int    _totalPatients;
    [ObservableProperty] private int    _todayPatients;
    [ObservableProperty] private int    _totalMedicines;
    [ObservableProperty] private int    _totalPhysicians;

    // ── Info cards ────────────────────────────────────────────────────────────
    [ObservableProperty] private string _lastBackupText    = "–";
    [ObservableProperty] private string _lastBackupSubText = "";
    [ObservableProperty] private bool   _hasBackup;
    [ObservableProperty] private string _databaseSizeText  = "–";
    [ObservableProperty] private string _todayDateText     = "";
    [ObservableProperty] private string _todayShamsiText   = "";

    // ── Recent visits ─────────────────────────────────────────────────────────
    [ObservableProperty]
    private ObservableCollection<RecentVisitRow> _recentVisits = [];

    // ── Feedback message ──────────────────────────────────────────────────────
    [ObservableProperty] private string _statusMessage = "";
    [ObservableProperty] private bool   _statusIsError;

    // ─────────────────────────────────────────────────────────────────────────

    [RelayCommand]
    public void Load()
    {
        StatusMessage = "";
        var db    = App.Db;
        var today = DateTime.Today;

        TotalPatients   = db.Patients.Count();
        TotalMedicines  = db.MedicineLists.Count();
        TotalPhysicians = db.Physicians.Count();

        // Today's visits (OpdDate between midnight and next midnight)
        var todayEnd = today.AddDays(1);
        TodayPatients = db.Patients.Count(p => p.OpdDate >= today && p.OpdDate < todayEnd);

        // Today's date labels
        TodayDateText   = today.ToString("dddd, MMMM d, yyyy", CultureInfo.InvariantCulture);
        TodayShamsiText = HijriService.ToShamsi(today);

        // Last backup info
        var backups = BackupService.ListBackups(BackupService.DefaultBackupFolder);
        if (backups.Count > 0)
        {
            HasBackup         = true;
            LastBackupText    = backups[0].CreatedText;
            LastBackupSubText = backups[0].SizeText;
        }
        else
        {
            HasBackup         = false;
            LastBackupText    = "No backups yet";
            LastBackupSubText = "";
        }

        // Database size
        try
        {
            var fi = new System.IO.FileInfo(App.DbPath);
            DatabaseSizeText = fi.Length < 1024 * 1024
                ? $"{fi.Length / 1024.0:F1} KB"
                : $"{fi.Length / (1024.0 * 1024):F2} MB";
        }
        catch { DatabaseSizeText = "–"; }

        // Recent visits (last 10 with a date)
        var rows = db.Patients
            .Where(p => p.OpdDate.HasValue)
            .OrderByDescending(p => p.OpdDate)
            .Take(10)
            .Select(p => new
            {
                p.PatientName,
                PhysName  = p.Physician != null ? p.Physician.NameEng : "",
                p.OpdDate,
                p.HijriDate,
                p.Diagnosis
            })
            .ToList();

        RecentVisits = new ObservableCollection<RecentVisitRow>(
            rows.Select(r => new RecentVisitRow(
                r.PatientName ?? "–",
                r.PhysName    ?? "–",
                r.OpdDate.HasValue ? r.OpdDate.Value.ToString("yyyy-MM-dd") : "",
                r.HijriDate   ?? "",
                r.Diagnosis   ?? "")));
    }

    [RelayCommand]
    public void CreateBackup()
    {
        try
        {
            var path = BackupService.CreateBackup(BackupService.DefaultBackupFolder);
            StatusIsError   = false;
            StatusMessage   = $"Backup created: {System.IO.Path.GetFileName(path)}";
            Load();   // refresh last-backup display
        }
        catch (Exception ex)
        {
            StatusIsError = true;
            StatusMessage = $"Backup failed: {ex.Message}";
        }
    }
}

/// <summary>Display-only row for the Recent Visits DataGrid.</summary>
public record RecentVisitRow(
    string PatientName,
    string Physician,
    string VisitDate,
    string ShamsiDate,
    string Diagnosis);
