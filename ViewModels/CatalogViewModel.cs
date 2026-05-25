using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OPDClinic.Data;
using OPDClinic.Models;

namespace OPDClinic.ViewModels;

public partial class CatalogViewModel : ObservableObject
{
    private readonly AppDbContext _db;
    private ObservableCollection<MedicineList> _allMedicines = [];
    private ICollectionView? _view;

    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private ICollectionView? _medicines;
    [ObservableProperty] private int    _totalCount;
    [ObservableProperty] private string _categoryFilter = "All";
    [ObservableProperty] private bool   _isLoading;

    public List<string> Categories { get; } = ["All", "Enteral", "Parenteral"];

    public CatalogViewModel(AppDbContext db)
    {
        _db = db;
        _ = LoadMedicines();
    }

    [RelayCommand]
    public async Task LoadMedicines()
    {
        IsLoading = true;
        try
        {
            await Task.Yield();
            _allMedicines = new ObservableCollection<MedicineList>(
                _db.MedicineLists.OrderBy(m => m.MedicineName).ToList());

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
                || m.Strength?.ToLower().Contains(s) == true;
        }

        return true;
    }

    private void RefreshCount() => TotalCount = _view?.Cast<object>().Count() ?? 0;

    partial void OnSearchTextChanged(string value)    { _view?.Refresh(); RefreshCount(); }
    partial void OnCategoryFilterChanged(string value) { _view?.Refresh(); RefreshCount(); }

    [RelayCommand]
    public void DeleteMedicine(MedicineList m)
    {
        var confirm = MessageBox.Show(
            $"Delete '{m.MedicineName}'?",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            _db.MedicineLists.Remove(m);
            _db.SaveChanges();
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
