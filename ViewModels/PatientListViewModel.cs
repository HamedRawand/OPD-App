using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using OPDClinic.Data;
using OPDClinic.Models;
using OPDClinic.Services;

namespace OPDClinic.ViewModels;

/// <summary>One row in the patient list — aggregates data across all visits for a patient.</summary>
public class PatientListRow
{
    public required Patient  Patient           { get; init; }
    public          int      VisitCount        { get; init; }
    public          DateTime? LastVisitDate    { get; init; }
    public          string?  LastDiagnosis     { get; init; }
    public          string?  LastPhysicianName { get; init; }

    // Shortcut properties used by XAML column bindings
    public string? PatientCode    => Patient.PatientCode;
    public string? PatientName    => Patient.PatientName;
    public string? Sex            => Patient.Sex;
    public string? PhoneNumber    => Patient.PhoneNumber;
    public string  LastVisitText  => LastVisitDate?.ToString("yyyy-MM-dd") ?? "—";
    public string  VisitCountText => VisitCount > 0
        ? $"{VisitCount} visit{(VisitCount == 1 ? "" : "s")}"
        : "No visits";
}

public partial class PatientListViewModel : ObservableObject
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private ObservableCollection<PatientListRow> _allRows = [];
    private ICollectionView? _patientsView;
    private const int MaxPatients = 500;

    // Debounce timer — prevents a CollectionView refresh on every keystroke
    private readonly DispatcherTimer _searchDebounce = new()
    {
        Interval = TimeSpan.FromMilliseconds(300)
    };

    [ObservableProperty] private string    _searchText = "";
    [ObservableProperty] private Physician? _selectedPhysician;
    [ObservableProperty] private DateTime? _filterDate;
    [ObservableProperty] private ICollectionView? _patients;
    [ObservableProperty] private ObservableCollection<Physician> _physicians = [];
    [ObservableProperty] private int    _totalCount;
    [ObservableProperty] private string _truncationNotice = "";
    [ObservableProperty] private bool   _isLoading;

    /// <summary>
    /// False for Doctors (auto-filtered to own patients — no point showing the picker).
    /// True for Admin and Receptionist.
    /// </summary>
    public bool ShowPhysicianFilter { get; } =
        App.Auth.Can(Permission.ViewAllPhysicianPatients);

    public PatientListViewModel(IDbContextFactory<AppDbContext> factory)
    {
        _factory = factory;
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
        using var db = _factory.CreateDbContext();
        var stubs = db.Physicians
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
            await Task.Yield();

            var currentUser = App.Auth.CurrentUser!;
            int? doctorPhysicianId = (!App.Auth.Can(Permission.ViewAllPhysicianPatients)
                                      && currentUser.PhysicianId.HasValue)
                                     ? currentUser.PhysicianId
                                     : null;

            using var db = _factory.CreateDbContext();

            // Base patient query — doctors only see their own patients
            var patientQuery = db.Patients.AsQueryable();
            if (doctorPhysicianId.HasValue)
                patientQuery = patientQuery
                    .Where(p => p.Visits.Any(v => v.PhysicianId == doctorPhysicianId.Value));

            var totalInDb = patientQuery.Count();

            // Project aggregate data: visit count, last visit date, last diagnosis, last physician
            var projected = patientQuery
                .Select(p => new
                {
                    Patient           = p,
                    VisitCount        = p.Visits.Count,
                    LastVisitDate     = p.Visits.Max(v => (DateTime?)v.OpdDate),
                    LastDiagnosis     = p.Visits
                        .OrderByDescending(v => v.OpdDate)
                        .Select(v => v.Diagnosis)
                        .FirstOrDefault(),
                    LastPhysicianName = p.Visits
                        .OrderByDescending(v => v.OpdDate)
                        .Select(v => v.Physician != null ? v.Physician.NameEng : null)
                        .FirstOrDefault()
                })
                .OrderByDescending(r => r.LastVisitDate)
                .Take(MaxPatients)
                .AsNoTracking()
                .ToList();

            TruncationNotice = totalInDb > MaxPatients
                ? $"Showing first {MaxPatients} of {totalInDb} records — use filters to narrow down."
                : "";

            var rows = projected.Select(r => new PatientListRow
            {
                Patient           = r.Patient,
                VisitCount        = r.VisitCount,
                LastVisitDate     = r.LastVisitDate,
                LastDiagnosis     = r.LastDiagnosis,
                LastPhysicianName = r.LastPhysicianName
            }).ToList();

            _allRows      = new ObservableCollection<PatientListRow>(rows);
            _patientsView = CollectionViewSource.GetDefaultView(_allRows);
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
        if (obj is not PatientListRow row) return false;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var s = SearchText.ToLower();
            bool match = row.PatientName?.ToLower().Contains(s)   == true
                      || row.PhoneNumber?.ToLower().Contains(s)   == true
                      || row.PatientCode?.ToLower().Contains(s)   == true
                      || row.LastDiagnosis?.ToLower().Contains(s) == true;
            if (!match) return false;
        }

        if (SelectedPhysician is not null &&
            row.LastPhysicianName != SelectedPhysician.NameEng)
            return false;

        if (FilterDate.HasValue &&
            (!row.LastVisitDate.HasValue || row.LastVisitDate.Value.Date != FilterDate.Value.Date))
            return false;

        return true;
    }

    // Debounced — waits 300 ms after the last keystroke before refreshing
    partial void OnSearchTextChanged(string value)
    {
        _searchDebounce.Stop();
        _searchDebounce.Start();
    }
    partial void OnSelectedPhysicianChanged(Physician? value) { _patientsView?.Refresh(); RefreshCount(); }
    partial void OnFilterDateChanged(DateTime? value)         { _patientsView?.Refresh(); RefreshCount(); }

    [RelayCommand]
    private void ClearFilters()
    {
        SearchText         = "";
        SelectedPhysician  = null;
        FilterDate         = null;
    }
}
