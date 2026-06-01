using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using OPDClinic.Data;
using OPDClinic.Models;
using OPDClinic.Services;

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
    private readonly IDbContextFactory<AppDbContext> _factory;
    private bool _suppressMedicineFilter;
    private bool _suppressFormFilter;

    // ── Role-based permission ──
    /// <summary>True for Admin and Doctor; false for Receptionist. Drives UI visibility.</summary>
    public bool CanWritePrescription { get; } = App.Auth.Can(Permission.WritePrescription);

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

    public PrescriptionViewModel(IDbContextFactory<AppDbContext> factory)
    {
        _factory = factory;

        // Load catalog data using a short-lived context
        using var db = factory.CreateDbContext();

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

    /// <summary>Loads medicines and lab tests for an existing visit.</summary>
    public void LoadExistingPrescription(int visitId)
    {
        using var db = _factory.CreateDbContext();

        var lines = db.MedicineUsages
            .Where(m => m.VisitId == visitId)
            .OrderBy(m => m.LineNumber)
            .ToList();

        foreach (var l in lines) Lines.Add(l);

        var orderedTestIds = db.PatientLabTests
            .Where(pt => pt.VisitId == visitId)
            .Select(pt => pt.LabTestId)
            .ToHashSet();

        foreach (var group in LabTestGroups)
            foreach (var test in group.Tests)
                if (orderedTestIds.Contains(test.Test.Id))
                    test.IsSelected = true;
    }

    /// <summary>
    /// Saves (replaces) all prescription lines and lab orders for a visit.
    /// Must be called within the caller's transaction — uses the provided <paramref name="db"/> context.
    /// </summary>
    public void SaveToDb(int visitId, AppDbContext db)
    {
        // Remove existing lines + lab orders
        var existingLines = db.MedicineUsages.Where(m => m.VisitId == visitId).ToList();
        db.MedicineUsages.RemoveRange(existingLines);

        var existingLabs = db.PatientLabTests.Where(l => l.VisitId == visitId).ToList();
        db.PatientLabTests.RemoveRange(existingLabs);

        // Save medicine lines — new objects (avoid EF identity-resolution bug)
        for (int i = 0; i < Lines.Count; i++)
        {
            var line = Lines[i];
            db.MedicineUsages.Add(new MedicineUsage
            {
                VisitId      = visitId,
                LineNumber   = i + 1,
                Prescription = line.Prescription,
                Type         = line.Type,
                Strength     = line.Strength,
                Qty          = line.Qty,
                Usage        = line.Usage,
                RouteName    = line.RouteName,
                Note         = line.Note,
            });
        }

        // Save selected lab tests
        foreach (var group in LabTestGroups)
            foreach (var test in group.Tests.Where(t => t.IsSelected))
                db.PatientLabTests.Add(new PatientLabTest
                {
                    VisitId   = visitId,
                    LabTestId = test.Test.Id
                });

        db.SaveChanges();
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
