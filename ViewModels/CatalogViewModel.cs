using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using OPDClinic.Data;
using OPDClinic.Models;

namespace OPDClinic.ViewModels;

public partial class CatalogViewModel : ObservableObject
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private ObservableCollection<MedicineList> _allMedicines = [];
    private ICollectionView? _view;

    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private ICollectionView? _medicines;
    [ObservableProperty] private int    _totalCount;
    [ObservableProperty] private string _categoryFilter = "All";
    [ObservableProperty] private bool   _isLoading;

    public List<string> Categories { get; } = ["All", "Enteral", "Parenteral"];

    public CatalogViewModel(IDbContextFactory<AppDbContext> factory)
    {
        _factory = factory;
        _ = LoadMedicines();
    }

    [RelayCommand]
    public async Task LoadMedicines()
    {
        IsLoading = true;
        try
        {
            await Task.Yield();
            using var db = _factory.CreateDbContext();
            _allMedicines = new ObservableCollection<MedicineList>(
                db.MedicineLists.Include(m => m.Strengths).OrderBy(m => m.MedicineName).ToList());

            _view = CollectionViewSource.GetDefaultView(_allMedicines);
            _view.Filter = ApplyFilter;
            Medicines = _view;
            RefreshCount();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool ApplyFilter(object obj)
    {
        if (obj is not MedicineList m) return false;

        if (CategoryFilter != "All" && m.Category != CategoryFilter)
            return false;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var s = SearchText.ToLower();
            return m.MedicineName?.ToLower().Contains(s) == true
                || m.GenericName?.ToLower().Contains(s) == true
                || m.Type?.ToLower().Contains(s) == true
                || m.StrengthsDisplay.ToLower().Contains(s);
        }

        return true;
    }

    private void RefreshCount() => TotalCount = _view?.Cast<object>().Count() ?? 0;

    partial void OnSearchTextChanged(string value)    { _view?.Refresh(); RefreshCount(); }
    partial void OnCategoryFilterChanged(string value) { _view?.Refresh(); RefreshCount(); }

    // ── Authorization properties for XAML bindings ───────────────────────────
    public bool CanAddMedicine    => App.Auth.Can(Services.Permission.AddMedicine);
    public bool CanEditMedicine   => App.Auth.Can(Services.Permission.EditMedicine);
    public bool CanDeleteMedicine => App.Auth.Can(Services.Permission.DeleteMedicineCatalog);
    public bool CanExportMedicine => App.Auth.Can(Services.Permission.ExportMedicineCatalog);

    [RelayCommand]
    public void DeleteMedicine(MedicineList m)
    {
        if (!App.Auth.Can(Services.Permission.DeleteMedicineCatalog))
        {
            MessageBox.Show("You do not have permission to delete medicines.",
                "Access Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var confirm = MessageBox.Show(
            $"Delete '{m.MedicineName}'?",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            using var db = _factory.CreateDbContext();
            db.Remove(m);
            db.SaveChanges();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not delete medicine:\n{ex.Message}",
                "Delete Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        _ = LoadMedicines();
    }
}
