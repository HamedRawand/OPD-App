using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using OPDClinic.Data;
using OPDClinic.Models;
using OPDClinic.Services;
using Serilog;

namespace OPDClinic.ViewModels;

public partial class PatientEditViewModel : ObservableObject
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private Patient? _existingPatient;
    private Visit?   _existingVisit;
    private bool     _updatingDate;

    [ObservableProperty] private string _windowTitle = "New Patient";
    [ObservableProperty] private bool   _isSaved;
    [ObservableProperty] private string? _errorMessage;

    // ── Permission gates (evaluated once at construction time) ────────────────
    public bool CanEnterClinicalData => App.Auth.Can(Permission.EnterClinicalData);
    public bool CanWritePrescription  => App.Auth.Can(Permission.WritePrescription);

    /// <summary>Set after a successful save — used by callers to open the detail view.</summary>
    public int SavedPatientId { get; private set; }
    public int SavedVisitId   { get; private set; }

    [ObservableProperty] private ObservableCollection<Physician> _physicians = [];
    [ObservableProperty] private Physician? _selectedPhysician;

    // ── Visit fields ──────────────────────────────────────────────────────────
    [ObservableProperty] private DateTime _opdDate = DateTime.Today;
    [ObservableProperty] private string   _shamsiDate = "";

    // ── Patient (demographic) fields ──────────────────────────────────────────
    [ObservableProperty] private string  _patientName = "";
    [ObservableProperty] private int?    _age;
    [ObservableProperty] private string  _sex = "مذکر";
    /// <summary>Auto-generated patient ID (e.g. "P-00042"). Read-only — displayed but never edited.</summary>
    [ObservableProperty] private string  _patientCode = "";
    /// <summary>Patient phone number.</summary>
    [ObservableProperty] private string  _phoneNumber = "";

    /// <summary>Drives the Sex ComboBox — Dari values stored directly in the DB.</summary>
    public string[] SexOptions { get; } = ["مذکر", "مؤنث"];

    // ── Vital signs (visit fields) ────────────────────────────────────────────
    private string _bp = ""; public string BP { get => _bp; set => SetProperty(ref _bp, value); }
    private string _hr = ""; public string HR { get => _hr; set => SetProperty(ref _hr, value); }
    private string _pr = ""; public string PR { get => _pr; set => SetProperty(ref _pr, value); }
    private string _rr = ""; public string RR { get => _rr; set => SetProperty(ref _rr, value); }
    private string _bt = ""; public string BT { get => _bt; set => SetProperty(ref _bt, value); }
    private string _bw = ""; public string BW { get => _bw; set => SetProperty(ref _bw, value); }

    [ObservableProperty] private string _clinicalFindings = "";
    [ObservableProperty] private string _diagnosis        = "";

    public PrescriptionViewModel Prescription { get; }

    /// <summary>
    /// Use this overload when creating a brand-new patient (and their first visit).
    /// </summary>
    public PatientEditViewModel(IDbContextFactory<AppDbContext> factory)
        : this(factory, null, null) { }

    /// <summary>
    /// Use this overload when adding a new visit to an existing patient.
    /// </summary>
    public PatientEditViewModel(IDbContextFactory<AppDbContext> factory, Patient patient)
        : this(factory, patient, null) { }

    /// <summary>
    /// Core constructor.
    /// <para><paramref name="patient"/> = null  → brand-new patient + new visit.</para>
    /// <para><paramref name="visit"/> = null     → new visit for an existing patient.</para>
    /// <para>Both provided                       → edit that specific visit.</para>
    /// </summary>
    public PatientEditViewModel(IDbContextFactory<AppDbContext> factory, Patient? patient, Visit? visit)
    {
        _factory         = factory;
        _existingPatient = patient;
        _existingVisit   = visit;

        Prescription = new PrescriptionViewModel(factory);

        // Load physician stubs using a short-lived context
        using var initDb = factory.CreateDbContext();
        var stubs = initDb.Physicians
            .OrderBy(p => p.NameEng)
            .Select(p => new { p.Id, p.NameEng })
            .ToList()
            .Select(x => new Physician { Id = x.Id, NameEng = x.NameEng })
            .ToList();
        Physicians = new ObservableCollection<Physician>(stubs);

        if (patient is not null)
        {
            LoadFromPatient(patient);

            if (visit is not null)
            {
                WindowTitle = $"{patient.PatientName} — Edit Visit ({visit.OpdDate?.ToString("yyyy-MM-dd") ?? "—"})";
                LoadFromVisit(visit);
                Prescription.LoadExistingPrescription(visit.Id);
                // Load footer note saved on this visit
                if (!string.IsNullOrEmpty(visit.FooterNote))
                    Prescription.SelectedPrescriptionNote =
                        Prescription.PrescriptionNotes
                            .FirstOrDefault(n => n.Notes == visit.FooterNote);
            }
            else
            {
                WindowTitle  = $"{patient.PatientName} — New Visit";
                ShamsiDate   = HijriService.ToShamsi(DateTime.Today);
            }
        }
        else
        {
            WindowTitle = "New Patient";
            ShamsiDate  = HijriService.ToShamsi(DateTime.Today);
        }
    }

    /// <summary>
    /// Pre-fills this new-visit form with data copied from a previous visit.
    /// Sets today's date, copies physician / diagnosis / findings / prescription lines / lab tests.
    /// Vitals are intentionally left blank (they must be taken fresh at each visit).
    /// Call after the default constructor (patient, null) has already run.
    /// </summary>
    public void RepeatFromVisit(Visit sourceVisit)
    {
        SelectedPhysician    = Physicians.FirstOrDefault(ph => ph.Id == sourceVisit.PhysicianId);
        Diagnosis            = sourceVisit.Diagnosis        ?? "";
        ClinicalFindings     = sourceVisit.ClinicalFindings ?? "";

        Prescription.LoadExistingPrescription(sourceVisit.Id);

        if (!string.IsNullOrEmpty(sourceVisit.FooterNote))
            Prescription.SelectedPrescriptionNote =
                Prescription.PrescriptionNotes
                    .FirstOrDefault(n => n.Notes == sourceVisit.FooterNote);
    }

    private void LoadFromPatient(Patient p)
    {
        PatientCode = p.PatientCode ?? "";
        PatientName = p.PatientName ?? "";
        Age         = null;               // Age is per-visit; will be overwritten by LoadFromVisit
        Sex         = NormalizeSex(p.Sex);
        PhoneNumber = p.PhoneNumber ?? "";
    }

    private void LoadFromVisit(Visit v)
    {
        SelectedPhysician = Physicians.FirstOrDefault(ph => ph.Id == v.PhysicianId);
        OpdDate           = v.OpdDate ?? DateTime.Today;
        ShamsiDate        = v.HijriDate ?? HijriService.ToShamsi(OpdDate);
        Age               = v.Age;
        BP                = v.BP ?? "";
        HR                = v.HR ?? "";
        PR                = v.PR ?? "";
        RR                = v.RR ?? "";
        BT                = v.BT ?? "";
        BW                = v.BW ?? "";
        ClinicalFindings  = v.ClinicalFindings ?? "";
        Diagnosis         = v.Diagnosis ?? "";
    }

    /// <summary>Normalises the Sex field to canonical Dari values ("مذکر" / "مؤنث").</summary>
    private static string NormalizeSex(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "مذکر";
        const string prefix = "System.Windows.Controls.ComboBoxItem: ";
        var clean = value.StartsWith(prefix, StringComparison.Ordinal)
            ? value[prefix.Length..] : value;
        return clean switch
        {
            "Male"   or "مذکر" => "مذکر",
            "Female" or "مؤنث" => "مؤنث",
            _                   => "مذکر"
        };
    }

    partial void OnOpdDateChanged(DateTime value)
    {
        if (_updatingDate) return;
        _updatingDate = true;
        ShamsiDate = HijriService.ToShamsi(value);
        _updatingDate = false;
    }

    partial void OnShamsiDateChanged(string value)
    {
        if (_updatingDate) return;
        var parsed = HijriService.FromShamsi(value);
        if (parsed.HasValue)
        {
            _updatingDate = true;
            OpdDate = parsed.Value;
            _updatingDate = false;
        }
    }

    [RelayCommand]
    private void Save()
    {
        ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(PatientName))
        {
            ErrorMessage = "Patient name is required.";
            return;
        }

        var isNewPatient = _existingPatient is null;
        var isNewVisit   = _existingVisit   is null;

        using var db = _factory.CreateDbContext();
        using var tx = db.Database.BeginTransaction();
        try
        {
            // ── 1. Patient (demographics) ─────────────────────────────────────
            Patient patient;
            if (isNewPatient)
            {
                patient = new Patient { CreatedAt = DateTime.UtcNow };
                db.Patients.Add(patient);
            }
            else
            {
                patient = db.Patients.Find(_existingPatient!.Id)
                          ?? throw new InvalidOperationException(
                                 $"Patient record (Id={_existingPatient.Id}) was not found.");
            }

            patient.PatientName = PatientName.Trim();
            patient.Sex         = Sex;
            patient.PhoneNumber = PhoneNumber.Trim();
            db.SaveChanges();

            // Auto-generate PatientCode for new patients
            if (isNewPatient && patient.PatientCode is null)
            {
                patient.PatientCode = $"P-{patient.Id:D5}";
                PatientCode = patient.PatientCode;
                db.SaveChanges();
            }

            // ── 2. Visit (clinical data) ──────────────────────────────────────
            Visit visit;
            if (isNewVisit)
            {
                visit = new Visit { PatientId = patient.Id };
                db.Visits.Add(visit);
            }
            else
            {
                visit = db.Visits.Find(_existingVisit!.Id)
                        ?? throw new InvalidOperationException(
                               $"Visit record (Id={_existingVisit.Id}) was not found.");
            }

            visit.PhysicianId = SelectedPhysician?.Id;
            visit.OpdDate     = OpdDate;
            visit.HijriDate   = ShamsiDate;

            // Clinical fields — only written by roles that hold EnterClinicalData
            if (CanEnterClinicalData)
            {
                visit.Age              = Age;
                visit.BP               = BP.Trim();
                visit.HR               = HR.Trim();
                visit.PR               = PR.Trim();
                visit.RR               = RR.Trim();
                visit.BT               = BT.Trim();
                visit.BW               = BW.Trim();
                visit.ClinicalFindings = ClinicalFindings.Trim();
                visit.Diagnosis        = Diagnosis.Trim();
            }
            if (App.Auth.Can(Permission.WritePrescription))
                visit.FooterNote = Prescription.SelectedPrescriptionNote?.Notes;
            visit.LastUpdated = DateTime.UtcNow;
            db.SaveChanges();

            // ── 3. Prescription lines + lab tests ─────────────────────────────
            SavedPatientId = patient.Id;
            SavedVisitId   = visit.Id;
            if (App.Auth.Can(Permission.WritePrescription))
                Prescription.SaveToDb(visit.Id, db);

            tx.Commit();
        }
        catch (Exception ex)
        {
            tx.Rollback();
            ErrorMessage = $"Save failed: {ex.Message}";
            return;
        }

        var action = isNewPatient ? "PatientCreated"
                   : isNewVisit   ? "VisitAdded"
                                  : "VisitUpdated";
        AuditService.Log(action, "Patient", SavedPatientId, PatientName);
        Log.Information("{Action} — PatientId:{Pid} VisitId:{Vid} Name:{Name}",
            action, SavedPatientId, SavedVisitId, PatientName);

        IsSaved = true;
    }
}
