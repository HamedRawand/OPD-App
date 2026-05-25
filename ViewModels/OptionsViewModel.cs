using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OPDClinic.Data;
using OPDClinic.Models;

namespace OPDClinic.ViewModels;

public partial class OptionsViewModel : ObservableObject
{
    private readonly AppDbContext _db;

    // ── Displayed collections ─────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<RouteOfAdministration> _routes             = [];
    [ObservableProperty] private ObservableCollection<Dosage>                _dosages            = [];
    [ObservableProperty] private ObservableCollection<MedicineForm>          _medicineCategories = [];
    [ObservableProperty] private ObservableCollection<MedicineNote>          _medicineNotes      = [];
    [ObservableProperty] private ObservableCollection<PrescriptionNote>      _prescriptionNotes  = [];
    [ObservableProperty] private ObservableCollection<LabTest>               _labTests           = [];

    /// <summary>Distinct category strings from Routes — drives the Category dropdowns
    /// in Dosage and Medicine Categories edit dialogs.</summary>
    [ObservableProperty] private List<string> _routeCategories = [];

    public OptionsViewModel(AppDbContext db)
    {
        _db = db;
        LoadAll();
    }

    private void LoadAll()
    {
        LoadRoutes();
        LoadDosages();
        LoadMedicineCategories();
        LoadMedicineNotes();
        LoadPrescriptionNotes();
        LoadLabTests();
    }

    private void RefreshRouteCategories()
    {
        RouteCategories = Routes
            .Select(r => r.Category)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct()
            .OrderBy(c => c)
            .ToList()!;
    }

    // ── Route of Administration ───────────────────────────────────────────────

    [RelayCommand]
    public void LoadRoutes()
    {
        Routes = new ObservableCollection<RouteOfAdministration>(
            _db.Routes.OrderBy(r => r.Category).ThenBy(r => r.RouteName).ToList());
        RefreshRouteCategories();
    }

    [RelayCommand]
    private void DeleteRoute(RouteOfAdministration item)
    {
        if (MessageBox.Show($"Delete route '{item.RouteName}'?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning)
            != MessageBoxResult.Yes) return;

        try
        {
            _db.Routes.Remove(item);
            _db.SaveChanges();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not delete route:\n{ex.Message}",
                "Delete Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        Routes.Remove(item);
        RefreshRouteCategories();
    }

    // ── Dosage ────────────────────────────────────────────────────────────────

    [RelayCommand]
    public void LoadDosages()
    {
        Dosages = new ObservableCollection<Dosage>(
            _db.Dosages.OrderBy(d => d.Category).ThenBy(d => d.DosageText).ToList());
    }

    [RelayCommand]
    private void DeleteDosage(Dosage item)
    {
        if (MessageBox.Show($"Delete dosage '{item.DosageText}'?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning)
            != MessageBoxResult.Yes) return;

        try
        {
            _db.Dosages.Remove(item);
            _db.SaveChanges();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not delete dosage:\n{ex.Message}",
                "Delete Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        Dosages.Remove(item);
    }

    // ── Medicine Categories (MedicineForm table) ──────────────────────────────

    [RelayCommand]
    public void LoadMedicineCategories()
    {
        MedicineCategories = new ObservableCollection<MedicineForm>(
            _db.MedicineForms.OrderBy(f => f.Category).ThenBy(f => f.FormName).ToList());
    }

    [RelayCommand]
    private void DeleteMedicineCategory(MedicineForm item)
    {
        if (MessageBox.Show($"Delete medicine form '{item.FormName}'?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning)
            != MessageBoxResult.Yes) return;

        try
        {
            _db.MedicineForms.Remove(item);
            _db.SaveChanges();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not delete medicine form:\n{ex.Message}",
                "Delete Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        MedicineCategories.Remove(item);
    }

    // ── Medicine Notes ────────────────────────────────────────────────────────

    [RelayCommand]
    public void LoadMedicineNotes()
    {
        MedicineNotes = new ObservableCollection<MedicineNote>(
            _db.MedicineNotes.OrderBy(n => n.Notes).ToList());
    }

    [RelayCommand]
    private void DeleteMedicineNote(MedicineNote item)
    {
        if (MessageBox.Show($"Delete medicine note '{item.Notes}'?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning)
            != MessageBoxResult.Yes) return;

        try
        {
            _db.MedicineNotes.Remove(item);
            _db.SaveChanges();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not delete medicine note:\n{ex.Message}",
                "Delete Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        MedicineNotes.Remove(item);
    }

    // ── Prescription Notes ────────────────────────────────────────────────────

    [RelayCommand]
    public void LoadPrescriptionNotes()
    {
        PrescriptionNotes = new ObservableCollection<PrescriptionNote>(
            _db.PrescriptionNotes.OrderBy(n => n.Notes).ToList());
    }

    [RelayCommand]
    private void DeletePrescriptionNote(PrescriptionNote item)
    {
        if (MessageBox.Show($"Delete prescription note '{item.Notes}'?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning)
            != MessageBoxResult.Yes) return;

        try
        {
            _db.PrescriptionNotes.Remove(item);
            _db.SaveChanges();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not delete prescription note:\n{ex.Message}",
                "Delete Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        PrescriptionNotes.Remove(item);
    }

    // ── Lab Tests ─────────────────────────────────────────────────────────────

    [RelayCommand]
    public void LoadLabTests()
    {
        LabTests = new ObservableCollection<LabTest>(
            _db.LabTests.OrderBy(t => t.Category).ThenBy(t => t.TestName).ToList());
    }

    [RelayCommand]
    private void DeleteLabTest(LabTest item)
    {
        if (MessageBox.Show($"Delete lab test '{item.TestName}'?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning)
            != MessageBoxResult.Yes) return;

        try
        {
            _db.LabTests.Remove(item);
            _db.SaveChanges();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not delete lab test:\n{ex.Message}",
                "Delete Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        LabTests.Remove(item);
    }
}
