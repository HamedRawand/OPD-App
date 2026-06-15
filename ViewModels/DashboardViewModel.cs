using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OPDClinic.Models;
using OPDClinic.Services;

namespace OPDClinic.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    // ── Stat cards ────────────────────────────────────────────────────────────
    [ObservableProperty] private int    _totalPatients;
    [ObservableProperty] private int    _todayVisits;
    [ObservableProperty] private int    _totalMedicines;
    [ObservableProperty] private int    _totalPhysicians;
    [ObservableProperty] private int    _totalVisits;

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
        using var db = App.DbFactory.CreateDbContext();
        var today = DateTime.Today;

        var currentUser = App.Auth.CurrentUser!;
        bool restrictToOwn = !App.Auth.Can(Permission.ViewAllPhysicianPatients);
        int? ownPhysicianId = currentUser.PhysicianId;

        // ── Stat counts ───────────────────────────────────────────────────────
        // Restricted users with no linked physician see nothing (not everything).
        IQueryable<Visit> visitQuery;
        if (!restrictToOwn)
            visitQuery = db.Visits.AsQueryable();
        else if (ownPhysicianId.HasValue)
            visitQuery = db.Visits.Where(v => v.PhysicianId == ownPhysicianId.Value);
        else
            visitQuery = db.Visits.Where(_ => false);

        // Total unique patients (filtered for doctors)
        if (!restrictToOwn)
            TotalPatients = db.Patients.Count();
        else if (ownPhysicianId.HasValue)
            TotalPatients = db.Patients.Count(p => p.Visits.Any(v => v.PhysicianId == ownPhysicianId.Value));
        else
            TotalPatients = 0;

        TotalMedicines  = db.MedicineLists.Count();
        TotalPhysicians = db.Physicians.Count();

        // All visits + today's visits
        TotalVisits = visitQuery.Count();
        var todayEnd = today.AddDays(1);
        TodayVisits = visitQuery.Count(v => v.OpdDate >= today && v.OpdDate < todayEnd);

        // ── Date labels ───────────────────────────────────────────────────────
        TodayDateText   = today.ToString("dddd, MMMM d, yyyy", CultureInfo.InvariantCulture);
        TodayShamsiText = HijriService.ToShamsi(today);

        // ── Last backup ───────────────────────────────────────────────────────
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

        // ── Database size ─────────────────────────────────────────────────────
        try
        {
            var fi = new System.IO.FileInfo(App.DbPath);
            DatabaseSizeText = fi.Length < 1024 * 1024
                ? $"{fi.Length / 1024.0:F1} KB"
                : $"{fi.Length / (1024.0 * 1024):F2} MB";
        }
        catch { DatabaseSizeText = "–"; }

        // ── Recent visits — one row per patient (latest visit), ordered by most recent ──
        // Step 1: group by patient, get visit count and latest date, take top 10
        var patientGroups = visitQuery
            .Where(v => v.OpdDate.HasValue)
            .GroupBy(v => v.PatientId)
            .Select(g => new
            {
                PatientId  = g.Key,
                VisitCount = g.Count(),
                LatestDate = g.Max(v => v.OpdDate)
            })
            .OrderByDescending(g => g.LatestDate)
            .Take(10)
            .ToList();

        // Step 2: load full visit details for those patients (latest visit per patient)
        var patientIds = patientGroups.Select(g => g.PatientId).ToList();
        var latestVisitDetails = visitQuery
            .Where(v => patientIds.Contains(v.PatientId) && v.OpdDate.HasValue)
            .OrderByDescending(v => v.OpdDate)
            .Select(v => new
            {
                v.PatientId,
                PatientName = v.Patient != null ? v.Patient.PatientName : "–",
                PhysName    = v.Physician != null ? v.Physician.NameEng : "",
                v.OpdDate,
                v.HijriDate,
                v.Diagnosis
            })
            .ToList()
            .GroupBy(v => v.PatientId)
            .ToDictionary(g => g.Key, g => g.First());

        RecentVisits = new ObservableCollection<RecentVisitRow>(
            patientGroups.Select(g =>
            {
                latestVisitDetails.TryGetValue(g.PatientId, out var d);
                return new RecentVisitRow(
                    g.PatientId,
                    d?.PatientName ?? "–",
                    d?.PhysName    ?? "–",
                    g.LatestDate.HasValue ? g.LatestDate.Value.ToString("yyyy-MM-dd") : "",
                    d?.HijriDate   ?? "",
                    d?.Diagnosis   ?? "",
                    g.VisitCount);
            }));
    }

    [RelayCommand]
    public void CreateBackup()
    {
        try
        {
            var path = BackupService.CreateBackup(BackupService.DefaultBackupFolder);
            StatusIsError   = false;
            StatusMessage   = $"Backup created: {System.IO.Path.GetFileName(path)}";
            Load();
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
    int    PatientId,
    string PatientName,
    string Physician,
    string VisitDate,
    string ShamsiDate,
    string Diagnosis,
    int    VisitCount);
