using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OPDClinic.Data;
using OPDClinic.Models;

namespace OPDClinic.ViewModels;

// Wrappers for lab test selection state
public partial class SelectableLabTest : ObservableObject
{
    public LabTest Test { get; }
    [ObservableProperty] private bool _isSelected;
    public SelectableLabTest(LabTest test) { Test = test; }
}

public class LabTestGroup(string category, IEnumerable<SelectableLabTest> tests)
{
    public string Category { get; } = category;
    public List<SelectableLabTest> Tests { get; } = [.. tests];
}

public partial class PrescriptionViewModel : ObservableObject
{
    private readonly AppDbContext _db;
    private bool _suppressMedicineFilter;
    private bool _suppressFormFilter;

    // ── Catalog data (read-only lists) ──
    public List<MedicineList>      AllMedicines      { get; }
    public List<Dosage>            AllDosages        { get; }
    public List<MedicineForm>      MedicineForms     { get; }
    public List<MedicineNote>      AllMedicineNotes  { get; }
    public List<PrescriptionNote>  PrescriptionNotes { get; }
    public List<LabTestGroup>      LabTestGroups     { get; }

    // ── Current prescription lines ──
    public ObservableCollection<MedicineUsage> Lines { get; } = [];

    // ── Medicine / Form fields ──
    [ObservableProperty] private ObservableCollection<MedicineList> _filteredMedicines = [];
    [ObservableProperty] private MedicineList?  _selectedMedicine;
    [ObservableProperty] private string         _currentMedicineName = "";
    [ObservableProperty] private MedicineForm?  _selectedMedicineForm;

    // ── Filtered dependent dropdowns ──
    [ObservableProperty] private ObservableCollection<Dosage>       _filteredDosages       = [];
    [ObservableProperty] private ObservableCollection<MedicineNote> _filteredMedicineNotes = [];

    // ── Other prescription fields ──
    [ObservableProperty] private string  _currentStrength = "";
    [ObservableProperty] private int?    _currentQty;
    [ObservableProperty] private Dosage?           _selectedDosage;
    [ObservableProperty] private MedicineNote?     _selectedMedicineNote;
    [ObservableProperty] private PrescriptionNote? _selectedPrescriptionNote;
    [ObservableProperty] private string?           _addError;

    public PrescriptionViewModel(AppDbContext db)
    {
        _db = db;

        AllMedicines      = db.MedicineLists.OrderBy(m => m.MedicineName).ToList();
        AllDosages        = db.Dosages.ToList();
        MedicineForms     = db.MedicineForms.OrderBy(f => f.FormName).ToList();
        AllMedicineNotes  = db.MedicineNotes.ToList();
        PrescriptionNotes = db.PrescriptionNotes.ToList();

        FilteredMedicines     = new ObservableCollection<MedicineList>(AllMedicines.Take(30));
        FilteredDosages       = new ObservableCollection<Dosage>(AllDosages);
        FilteredMedicineNotes = new ObservableCollection<MedicineNote>(AllMedicineNotes);

        LabTestGroups = db.LabTests
            .OrderBy(t => t.Category).ThenBy(t => t.TestName)
            .ToList()
            .GroupBy(t => t.Category ?? "Other")
            .Select(g => new LabTestGroup(g.Key, g.Select(t => new SelectableLabTest(t))))
            .ToList();

        // Subscribe to each test's IsSelected so SelectedLabTests stays reactive
        foreach (var test in LabTestGroups.SelectMany(g => g.Tests))
            test.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(SelectableLabTest.IsSelected))
                {
                    OnPropertyChanged(nameof(SelectedLabTests));
                    OnPropertyChanged(nameof(SelectedLabTestGroups));
                }
            };
    }

    // ── Form selection → cascade-filter medicine, dosage, note ──────────────
    partial void OnSelectedMedicineFormChanged(MedicineForm? value)
    {
        // Always refresh dosage + note filter
        UpdateFilteredDosages(value);
        UpdateFilteredMedicineNotes(value);

        if (_suppressFormFilter) return;

        // Manual form change: clear medicine selection and re-filter list
        _suppressMedicineFilter = true;
        try
        {
            SelectedMedicine     = null;
            CurrentMedicineName  = "";
            SelectedDosage       = null;
            SelectedMedicineNote = null;
        }
        finally { _suppressMedicineFilter = false; }

        FilteredMedicines = value is null
            ? new ObservableCollection<MedicineList>(AllMedicines.Take(30))
            : new ObservableCollection<MedicineList>(
                AllMedicines.Where(m => m.Type == value.FormName).Take(50));
    }

    // ── Auto-filter medicines as user types ──────────────────────────────────
    partial void OnCurrentMedicineNameChanged(string value)
    {
        if (_suppressMedicineFilter) return;
        var lower = value.ToLower();
        var source = SelectedMedicineForm is null
            ? AllMedicines
            : AllMedicines.Where(m => m.Type == SelectedMedicineForm.FormName);
        FilteredMedicines = new ObservableCollection<MedicineList>(
            source
                .Where(m => string.IsNullOrEmpty(lower)
                         || m.MedicineName?.ToLower().Contains(lower) == true
                         || m.GenericName?.ToLower().Contains(lower) == true)
                .Take(25));
    }

    // ── Auto-fill strength + form when a medicine is selected ────────────────
    partial void OnSelectedMedicineChanged(MedicineList? value)
    {
        if (value is null) return;
        _suppressMedicineFilter = true;
        _suppressFormFilter     = true;
        try
        {
            CurrentMedicineName  = value.MedicineName ?? "";
            CurrentStrength      = value.Strength     ?? "";
            SelectedMedicineForm = MedicineForms.FirstOrDefault(f => f.FormName == value.Type);
        }
        finally
        {
            _suppressMedicineFilter = false;
            _suppressFormFilter     = false;
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────
    private void UpdateFilteredDosages(MedicineForm? form)
    {
        FilteredDosages = form is null
            ? new ObservableCollection<Dosage>(AllDosages)
            : new ObservableCollection<Dosage>(
                AllDosages.Where(d => d.Category == form.Category
                               || string.IsNullOrEmpty(d.Category)));
    }

    private void UpdateFilteredMedicineNotes(MedicineForm? form)
    {
        FilteredMedicineNotes = form is null
            ? new ObservableCollection<MedicineNote>(AllMedicineNotes)
            : new ObservableCollection<MedicineNote>(
                AllMedicineNotes.Where(n => n.Category == form.Category
                                       || string.IsNullOrEmpty(n.Category)));
    }

    // ── Add / Remove lines ───────────────────────────────────────────────────
    [RelayCommand]
    private void AddLine()
    {
        AddError = null;
        if (string.IsNullOrWhiteSpace(CurrentMedicineName))
        {
            AddError = "Medicine name is required.";
            return;
        }

        var name = CurrentMedicineName.Trim();
        if (Lines.Any(l => string.Equals(l.Prescription, name, StringComparison.OrdinalIgnoreCase)))
        {
            AddError = $"'{name}' is already in the prescription.";
            return;
        }

        Lines.Add(new MedicineUsage
        {
            LineNumber   = Lines.Count + 1,
            Prescription = name,
            Type         = SelectedMedicineForm?.FormName,
            Strength     = CurrentStrength.Trim(),
            Qty          = CurrentQty,
            Usage        = SelectedDosage?.DosageText,
            Note         = SelectedMedicineNote?.Notes,
        });

        ClearAddForm();
    }

    [RelayCommand]
    private void RemoveLine(MedicineUsage line)
    {
        Lines.Remove(line);
        for (int i = 0; i < Lines.Count; i++)
            Lines[i].LineNumber = i + 1;
    }

    [RelayCommand]
    private void ClearPrescriptionNote() => SelectedPrescriptionNote = null;

    private void ClearAddForm()
    {
        CurrentMedicineName  = "";
        CurrentStrength      = "";
        CurrentQty           = null;
        SelectedMedicineForm = null;
        SelectedDosage       = null;
        SelectedMedicineNote = null;
        SelectedMedicine     = null;
        AddError             = null;
    }

    // ── Load / Save ──────────────────────────────────────────────────────────
    public void LoadExistingPrescription(int patientId)
    {
        var lines = _db.MedicineUsages
            .Where(m => m.PatientId == patientId)
            .OrderBy(m => m.LineNumber)
            .ToList();

        foreach (var l in lines) Lines.Add(l);

        var orderedTestIds = _db.PatientLabTests
            .Where(pt => pt.PatientId == patientId)
            .Select(pt => pt.LabTestId)
            .ToHashSet();

        foreach (var group in LabTestGroups)
            foreach (var test in group.Tests)
                if (orderedTestIds.Contains(test.Test.Id))
                    test.IsSelected = true;

        // Load saved footer note — match by text so preset deletions don't break old records
        var patient = _db.Patients.Find(patientId);
        if (!string.IsNullOrEmpty(patient?.FooterNote))
            SelectedPrescriptionNote = PrescriptionNotes
                .FirstOrDefault(n => n.Notes == patient.FooterNote);
    }

    public void SaveToDb(int patientId)
    {
        // Remove existing lines + lab orders
        var existingLines = _db.MedicineUsages.Where(m => m.PatientId == patientId).ToList();
        _db.MedicineUsages.RemoveRange(existingLines);

        var existingLabs = _db.PatientLabTests.Where(l => l.PatientId == patientId).ToList();
        _db.PatientLabTests.RemoveRange(existingLabs);

        // Save medicine lines — new objects (avoid EF identity-resolution bug)
        for (int i = 0; i < Lines.Count; i++)
        {
            var line = Lines[i];
            _db.MedicineUsages.Add(new MedicineUsage
            {
                PatientId    = patientId,
                LineNumber   = i + 1,
                Prescription = line.Prescription,
                Type         = line.Type,
                Strength     = line.Strength,
                Qty          = line.Qty,
                Usage        = line.Usage,
                RouteName    = line.RouteName,   // kept for backward-compat with imported data
                Note         = line.Note,
            });
        }

        // Save selected lab tests
        foreach (var group in LabTestGroups)
            foreach (var test in group.Tests.Where(t => t.IsSelected))
                _db.PatientLabTests.Add(new PatientLabTest
                {
                    PatientId = patientId,
                    LabTestId = test.Test.Id
                });

        _db.SaveChanges();
    }

    // ── Computed ─────────────────────────────────────────────────────────────

    /// <summary>Flat selected list — used by SaveToDb and for count checks.</summary>
    public List<SelectableLabTest> SelectedLabTests =>
        LabTestGroups.SelectMany(g => g.Tests).Where(t => t.IsSelected).ToList();

    /// <summary>Selected tests grouped by category — drives the grouped chips panel in the form.</summary>
    public List<LabTestGroup> SelectedLabTestGroups =>
        LabTestGroups
            .Select(g => new LabTestGroup(g.Category, g.Tests.Where(t => t.IsSelected)))
            .Where(g => g.Tests.Count > 0)
            .ToList();
}
