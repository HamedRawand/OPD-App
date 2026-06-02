using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using OPDClinic.Data;
using OPDClinic.Models;

namespace OPDClinic.ViewModels;

/// <summary>One point in the vitals trend chart (chronological order).</summary>
public record VitalsPoint(DateTime Date, double? Bw, double? SysBp);

/// <summary>One card in the visit timeline shown in PatientDetailWindow.</summary>
public partial class VisitListRow : ObservableObject
{
    public Visit   Visit   { get; }
    public Patient Patient { get; }

    /// <summary>Drives the ▾ Details / ▴ Hide toggle on the card.</summary>
    [ObservableProperty] private bool _isExpanded;

    public VisitListRow(Visit visit, Patient patient)
    {
        Visit   = visit;
        Patient = patient;
    }

    // ── Core bindings ─────────────────────────────────────────────────────────
    public int     VisitId       => Visit.Id;
    public string  DateText      => Visit.OpdDate?.ToString("yyyy-MM-dd") ?? "—";
    public string  ShamsiText    => Visit.HijriDate ?? "—";
    public string  PhysicianName => Visit.Physician?.NameEng ?? "—";
    public int?    Age           => Visit.Age;
    public string  Diagnosis     => string.IsNullOrWhiteSpace(Visit.Diagnosis) ? "—" : Visit.Diagnosis;

    /// <summary>Null when empty so NullToCollapsed hides the block.</summary>
    public string? ClinicalFindings =>
        string.IsNullOrWhiteSpace(Visit.ClinicalFindings) ? null : Visit.ClinicalFindings;

    // ── Timeline stats chips ──────────────────────────────────────────────────
    public int  RxCount  => Visit.Medicines?.Count ?? 0;
    public int  LabCount => Visit.LabTests?.Count  ?? 0;
    public bool HasRx    => RxCount  > 0;
    public bool HasLabs  => LabCount > 0;

    // ── Expanded details ──────────────────────────────────────────────────────
    public bool HasVitals =>
        !string.IsNullOrWhiteSpace(Visit.BP) || !string.IsNullOrWhiteSpace(Visit.HR) ||
        !string.IsNullOrWhiteSpace(Visit.PR) || !string.IsNullOrWhiteSpace(Visit.RR) ||
        !string.IsNullOrWhiteSpace(Visit.BT) || !string.IsNullOrWhiteSpace(Visit.BW);

    public string VitalsSummary
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(Visit.BP)) parts.Add($"BP  {Visit.BP}");
            if (!string.IsNullOrWhiteSpace(Visit.HR)) parts.Add($"HR  {Visit.HR}");
            if (!string.IsNullOrWhiteSpace(Visit.PR)) parts.Add($"PR  {Visit.PR}");
            if (!string.IsNullOrWhiteSpace(Visit.RR)) parts.Add($"RR  {Visit.RR}");
            if (!string.IsNullOrWhiteSpace(Visit.BT)) parts.Add($"BT  {Visit.BT}");
            if (!string.IsNullOrWhiteSpace(Visit.BW)) parts.Add($"BW  {Visit.BW}");
            return string.Join("   ·   ", parts);
        }
    }

    /// <summary>One formatted string per medicine line for the expanded card.</summary>
    public IEnumerable<string> RxLineSummaries =>
        Visit.Medicines?
            .OrderBy(m => m.LineNumber)
            .Select(m =>
            {
                var seg = new List<string>();
                if (!string.IsNullOrWhiteSpace(m.Type))         seg.Add(m.Type!);
                if (!string.IsNullOrWhiteSpace(m.Prescription)) seg.Add(m.Prescription!);
                if (!string.IsNullOrWhiteSpace(m.Strength))     seg.Add(m.Strength!);
                var line = $"{m.LineNumber}.  {string.Join("  ", seg)}";
                if (m.Qty.HasValue) line += $"  ×  {m.Qty}";
                return line;
            })
        ?? [];

    /// <summary>Dot-joined test names for the expanded card footer.</summary>
    public string LabTestsSummary
    {
        get
        {
            if (Visit.LabTests is null || !Visit.LabTests.Any()) return "";
            var names = Visit.LabTests
                .Where(lt => !string.IsNullOrWhiteSpace(lt.LabTest?.TestName))
                .Select(lt => lt.LabTest!.TestName!);
            return "Labs:   " + string.Join("  ·  ", names);
        }
    }
}

public partial class PatientDetailViewModel : ObservableObject
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public Patient Patient { get; }

    /// <summary>Display text for the patient header.</summary>
    public string PatientHeader =>
        $"{Patient.PatientName}  ·  {Patient.PatientCode ?? "—"}";

    [ObservableProperty] private ObservableCollection<VisitListRow> _visits = [];
    [ObservableProperty] private string _summaryText = "";
    [ObservableProperty] private bool   _hasVitalsChart;

    /// <summary>Chronologically ordered vitals data for the trend chart.</summary>
    public List<VitalsPoint> VitalsHistory { get; private set; } = [];

    public PatientDetailViewModel(IDbContextFactory<AppDbContext> factory, Patient patient)
    {
        _factory = factory;
        Patient  = patient;
        LoadVisits();
    }

    [RelayCommand]
    public void LoadVisits()
    {
        using var db = _factory.CreateDbContext();

        var visits = db.Visits
            .Include(v => v.Physician)
            .Include(v => v.Medicines)
            .Include(v => v.LabTests).ThenInclude(lt => lt.LabTest)
            .Where(v => v.PatientId == Patient.Id)
            .OrderByDescending(v => v.OpdDate)
            .AsNoTracking()
            .ToList();

        Visits = new ObservableCollection<VisitListRow>(
            visits.Select(v => new VisitListRow(v, Patient)));

        // ── Summary stats ──────────────────────────────────────────────────────
        if (visits.Count == 0)
        {
            SummaryText = "No visits recorded";
        }
        else if (visits.Count == 1)
        {
            var d = visits[0].OpdDate?.ToString("yyyy-MM-dd") ?? "—";
            SummaryText = $"1 visit  ·  {d}";
        }
        else
        {
            var first  = visits.MinBy(v => v.OpdDate)?.OpdDate?.ToString("yyyy-MM-dd") ?? "—";
            var latest = visits.MaxBy(v => v.OpdDate)?.OpdDate?.ToString("yyyy-MM-dd") ?? "—";
            SummaryText = $"{visits.Count} visits  ·  First: {first}  ·  Latest: {latest}";
        }

        // ── Vitals trend data (chronological for the chart) ───────────────────
        VitalsHistory = visits
            .Where(v => v.OpdDate.HasValue)
            .OrderBy(v => v.OpdDate)
            .Select(v => new VitalsPoint(
                v.OpdDate!.Value,
                ParseBw(v.BW),
                ParseSysBp(v.BP)))
            .ToList();

        var chartable = VitalsHistory.Count(p => p.Bw.HasValue || p.SysBp.HasValue);
        HasVitalsChart = chartable >= 2;
    }

    // ── Parsing helpers ───────────────────────────────────────────────────────

    /// <summary>Parse body weight — strips "kg", "KG" suffix and whitespace.</summary>
    private static double? ParseBw(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        s = s.Trim().ToLowerInvariant().Replace("kg", "").Trim();
        return double.TryParse(s, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : null;
    }

    /// <summary>Parse systolic blood pressure — takes the part before "/" in "120/80".</summary>
    private static double? ParseSysBp(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var part = s.Split('/')[0].Trim();
        return double.TryParse(part, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : null;
    }
}
