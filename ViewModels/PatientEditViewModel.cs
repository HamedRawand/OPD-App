using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OPDClinic.Data;
using OPDClinic.Models;
using OPDClinic.Services;
using Serilog;

namespace OPDClinic.ViewModels;

public partial class PatientEditViewModel : ObservableObject
{
    private readonly AppDbContext _db;
    private Patient? _existingPatient;
    private bool _updatingDate;

    [ObservableProperty] private string _windowTitle = "New Patient Visit";
    [ObservableProperty] private bool _isSaved;
    [ObservableProperty] private string? _errorMessage;
    public int SavedPatientId { get; private set; }

    [ObservableProperty] private ObservableCollection<Physician> _physicians = [];
    [ObservableProperty] private Physician? _selectedPhysician;

    [ObservableProperty] private DateTime _opdDate = DateTime.Today;
    [ObservableProperty] private string _shamsiDate = "";

    [ObservableProperty] private string _patientName = "";
    [ObservableProperty] private int? _age;
    [ObservableProperty] private string _sex = "مذکر";
    [ObservableProperty] private string _patientNumber = "";

    /// <summary>Drives the Sex ComboBox — Dari values stored directly in the DB.</summary>
    public string[] SexOptions { get; } = ["مذکر", "مؤنث"];

    private string _bp = ""; public string BP { get => _bp; set => SetProperty(ref _bp, value); }
    private string _hr = ""; public string HR { get => _hr; set => SetProperty(ref _hr, value); }
    private string _pr = ""; public string PR { get => _pr; set => SetProperty(ref _pr, value); }
    private string _rr = ""; public string RR { get => _rr; set => SetProperty(ref _rr, value); }
    private string _bt = ""; public string BT { get => _bt; set => SetProperty(ref _bt, value); }
    private string _bw = ""; public string BW { get => _bw; set => SetProperty(ref _bw, value); }

    [ObservableProperty] private string _clinicalFindings = "";
    [ObservableProperty] private string _diagnosis = "";

    public PrescriptionViewModel Prescription { get; }

    public PatientEditViewModel(AppDbContext db, Patient? patient = null)
    {
        _db = db;
        Prescription = new PrescriptionViewModel(db);

        // Project to stubs — avoids loading SymbolImage BLOBs for every physician
        // just to populate the ComboBox in the patient form.
        var stubs = db.Physicians
            .OrderBy(p => p.NameEng)
            .Select(p => new { p.Id, p.NameEng })
            .ToList()
            .Select(x => new Physician { Id = x.Id, NameEng = x.NameEng })
            .ToList();
        Physicians = new ObservableCollection<Physician>(stubs);

        if (patient is not null)
        {
            _existingPatient = patient;
            WindowTitle = $"{patient.PatientName} — Edit Visit";
            LoadFromPatient(patient);
            Prescription.LoadExistingPrescription(patient.Id);
        }
        else
        {
            ShamsiDate = HijriService.ToShamsi(DateTime.Today);
        }
    }

    private void LoadFromPatient(Patient p)
    {
        SelectedPhysician  = Physicians.FirstOrDefault(ph => ph.Id == p.PhysicianId);
        OpdDate            = p.OpdDate ?? DateTime.Today;
        ShamsiDate         = p.HijriDate ?? HijriService.ToShamsi(OpdDate);
        PatientName        = p.PatientName ?? "";
        Age                = p.Age;
        Sex                = NormalizeSex(p.Sex);
        PatientNumber      = p.PatientNumber ?? "";
        BP                 = p.BP ?? "";
        HR                 = p.HR ?? "";
        PR                 = p.PR ?? "";
        RR                 = p.RR ?? "";
        BT                 = p.BT ?? "";
        BW                 = p.BW ?? "";
        ClinicalFindings   = p.ClinicalFindings ?? "";
        Diagnosis          = p.Diagnosis ?? "";
    }

    /// <summary>Normalises the Sex field to canonical Dari values ("مذکر" / "مؤنث").
    /// Handles: legacy English ("Male"/"Female"), old WPF ComboBoxItem prefix, and
    /// already-correct Dari values.</summary>
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

        var isNew = _existingPatient is null;

        // For existing patients the list loads with AsNoTracking(), so _existingPatient is
        // NOT tracked by EF.  SaveChanges() only saves tracked entities, which means using
        // _existingPatient directly would silently discard every edit.
        // Find() first checks the EF identity map (free hit if LoadExistingPrescription
        // already loaded it), otherwise executes a single-row SELECT and begins tracking.
        // Either way we get the tracked entity and EF detects every property change on it.
        Patient patient;
        if (isNew)
        {
            patient = new Patient();
        }
        else
        {
            patient = _db.Patients.Find(_existingPatient!.Id)
                      ?? throw new InvalidOperationException(
                             $"Patient record (Id={_existingPatient.Id}) was not found in the database.");
        }

        patient.PhysicianId      = SelectedPhysician?.Id;
        patient.OpdDate          = OpdDate;
        patient.HijriDate        = ShamsiDate;
        patient.PatientName      = PatientName.Trim();
        patient.Age              = Age;
        patient.Sex              = Sex;
        patient.PatientNumber    = PatientNumber.Trim();
        patient.BP               = BP.Trim();
        patient.HR               = HR.Trim();
        patient.PR               = PR.Trim();
        patient.RR               = RR.Trim();
        patient.BT               = BT.Trim();
        patient.BW               = BW.Trim();
        patient.ClinicalFindings = ClinicalFindings.Trim();
        patient.Diagnosis        = Diagnosis.Trim();
        patient.FooterNote       = Prescription.SelectedPrescriptionNote?.Notes;
        patient.LastUpdated      = DateTime.UtcNow;

        using var tx = _db.Database.BeginTransaction();
        try
        {
            if (isNew) _db.Patients.Add(patient);
            _db.SaveChanges();

            SavedPatientId = patient.Id;
            Prescription.SaveToDb(patient.Id);

            tx.Commit();
        }
        catch (Exception ex)
        {
            tx.Rollback();
            ErrorMessage = $"Save failed: {ex.Message}";
            return;
        }

        AuditService.Log(
            isNew ? "PatientCreated" : "PatientUpdated",
            "Patient", patient.Id,
            patient.PatientName);
        Log.Information("{Action} — Id:{Id} Name:{Name}",
            isNew ? "PatientCreated" : "PatientUpdated",
            patient.Id, patient.PatientName);

        IsSaved = true;
    }
}
