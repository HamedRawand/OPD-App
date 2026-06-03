using System.Windows;
using System.Windows.Controls;
using OPDClinic.Models;
using OPDClinic.Services;
using OPDClinic.ViewModels;

namespace OPDClinic.Views;

public partial class PhysicianView : UserControl
{
    public PhysicianViewModel ViewModel { get; }

    public PhysicianView()
    {
        InitializeComponent();
        ViewModel = new PhysicianViewModel(App.DbFactory);
        DataContext = ViewModel;
    }

    private void AddPhysician_Click(object sender, RoutedEventArgs e)
    {
        if (!App.Auth.Can(Services.Permission.AddPhysician)) return;
        var dlg = new PhysicianEditDialog(App.DbFactory) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true)
            ViewModel.LoadPhysiciansCommand.Execute(null);
    }

    private void EditPhysician_Click(object sender, RoutedEventArgs e)
    {
        if (!App.Auth.Can(Services.Permission.EditPhysician)) return;
        if (sender is Button btn && btn.Tag is Physician physician)
        {
            var dlg = new PhysicianEditDialog(App.DbFactory, physician) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() == true)
                ViewModel.LoadPhysiciansCommand.Execute(null);
        }
    }

    private void ExportPhysicians_Click(object sender, RoutedEventArgs e)
    {
        if (!App.Auth.Can(Services.Permission.ExportPhysicians)) return;
        ExportService.ExportPhysicians(ViewModel.Physicians);
    }
}
