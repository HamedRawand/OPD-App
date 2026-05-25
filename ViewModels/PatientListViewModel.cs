using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using OPDClinic.Data;
using OPDClinic.Models;

namespace OPDClinic.ViewModels;

public partial class PatientListViewModel : ObservableObject
{
    private readonly AppDbContext _db;
    private ObservableCollection<Patient> _allPatients = [];
    private ICollectionView? _patientsView;
    private const int MaxPatients = 500;

    // Debounce timer — prevents a CollectionView refresh on every keystroke
    private readonly DispatcherTimer _searchDebounce = new()
    {
        Interval = TimeSpan.FromMilliseconds(300)
    };

    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private Physician? _selectedPhysician;
    [ObservableProperty] private DateTime? _filterDate;
    [ObservableProperty] private ICollectionView? _patients;
    [ObservableProperty] private ObservableCollection<Physician> _physicians = [];
    [ObservableProperty] private int    _totalCount;
    [ObservableProperty] private string _truncationNotice = "";
    [ObservableProperty] private bool   _isLoading;

    public PatientListViewModel(AppDbContext db)
    {
        _db = db;
        _searchDebounce.Tick += (_, _) =>
        {
            _searchDebounce.Stop();
            _patientsView?.Refresh();
            RefreshCount();
        };
        LoadPhysicians();
        _ = LoadPatients();
    }

    private void LoadPhysicians()
    {
        // Project to avoid loading SymbolImage BLOB — only need Id + NameEng for the filter ComboBox
        var stubs = _db.Physicians
            .OrderBy(p => p.NameEng)
            .Select(p => new { p.Id, p.NameEng })
            .ToList()
            .Select(x => new Physician { Id = x.Id, NameEng = x.NameEng })
            .ToList();
        Physicians = new ObservableCollection<Physician>(stubs);
    }

    [RelayCommand]
    public async Task LoadPatients()
    {
        IsLoading = true;
        try
        {
            // Yield to let the UI render the loading indicator before blocking on DB
            await Task.Yield();

            var totalInDb = _db.Patients.Count();

            // Load patients without .Include(Physician) to avoid pulling the SymbolImage BLOB
            // for every row.  Physician stubs (Id + NameEng only) are attached manually below.
            var patients = _db.Patients
                .OrderByDescending(p => p.OpdDate)
                .Take(MaxPatients)
                .AsNoTracking()
                .ToList();

            // Build a lightweight physician lookup — name only, no BLOB
            var physicianLookup = _db.Physicians
                .Select(p => new { p.Id, p.NameEng })
                .ToDictionary(x => x.Id, x => x.NameEng);

            foreach (var p in patients)
            {
                // Normalise to canonical Dari ("مذکر"/"مؤنث"), handles old English + ComboBoxItem prefix
                p.Sex = NormalizeSex(p.Sex);

                if (p.PhysicianId.HasValue &&
                    physicianLookup.TryGetValue(p.PhysicianId.Value, out var name))
                {
                    p.Physician = new Physician { Id = p.PhysicianId.Value, NameEng = name };
                }
            }

            TruncationNotice = totalInDb > MaxPatients
                ? $"Showing first {MaxPatients} of {totalInDb} records — use filters to narrow down."
                : "";

            _allPatients = new ObservableCollection<Patient>(patients);
            _patientsView = CollectionViewSource.GetDefaultView(_allPatients);
            _patientsView.Filter = ApplyFilter;
            Patients = _patientsView;
            RefreshCount();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void RefreshCount() =>
        TotalCount = _patientsView?.Cast<object>().Count() ?? 0;

    private bool ApplyFilter(object obj)
    {
        if (obj is not Patient p) return false;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var s = SearchText.ToLower();
            bool match = p.PatientName?.ToLower().Contains(s) == true
                      || p.PatientNumber?.ToLower().Contains(s) == true
                      || p.Diagnosis?.ToLower().Contains(s) == true;
            if (!match) return false;
        }

        if (SelectedPhysician is not null && p.PhysicianId != SelectedPhysician.Id)
            return false;

        if (FilterDate.HasValue && p.OpdDate.HasValue &&
            p.OpdDate.Value.Date != FilterDate.Value.Date)
            return false;

        return true;
    }

    // Debounced — waits 300 ms after the last keystroke before refreshing
    partial void OnSearchTextChanged(string value)
    {
        _searchDebounce.Stop();
        _searchDebounce.Start();
    }
    // Discrete selection changes — refresh immediately
    partial void OnSelectedPhysicianChanged(Physician? value) { _patientsView?.Refresh(); RefreshCount(); }
    partial void OnFilterDateChanged(DateTime? value)         { _patientsView?.Refresh(); RefreshCount(); }

    [RelayCommand]
    private void ClearFilters()
    {
        SearchText = "";
        SelectedPhysician = null;
        FilterDate = null;
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
}
