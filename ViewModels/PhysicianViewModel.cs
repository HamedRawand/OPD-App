using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OPDClinic.Data;
using OPDClinic.Models;
using OPDClinic.Services;

namespace OPDClinic.ViewModels;

public partial class PhysicianViewModel : ObservableObject
{
    private readonly AppDbContext _db;

    [ObservableProperty] private ObservableCollection<Physician> _physicians = [];
    [ObservableProperty] private int _physicianCount;

    public PhysicianViewModel(AppDbContext db)
    {
        _db = db;
        LoadPhysicians();
    }

    [RelayCommand]
    public void LoadPhysicians()
    {
        Physicians = new ObservableCollection<Physician>(
            _db.Physicians.OrderBy(p => p.NameEng).ToList());
        PhysicianCount = Physicians.Count;
    }

    [RelayCommand]
    public void DeletePhysician(Physician p)
    {
        var confirm = MessageBox.Show(
            $"Delete physician '{p.NameEng}'?\n\nThis will not delete existing patient records.",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            _db.Physicians.Remove(p);
            _db.SaveChanges();
            AuditService.Log("PhysicianDeleted", "Physician", p.Id, p.NameEng);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not delete physician:\n{ex.Message}",
                "Delete Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        LoadPhysicians();
    }
}
