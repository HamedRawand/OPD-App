using System.Windows;
using System.Windows.Controls;
using OPDClinic.Models;
using OPDClinic.ViewModels;

namespace OPDClinic.Views;

public partial class PhysicianView : UserControl
{
    public PhysicianViewModel ViewModel { get; }

    public PhysicianView()
    {
        InitializeComponent();
        ViewModel = new PhysicianViewModel(App.Db);
        DataContext = ViewModel;
    }

    private void AddPhysician_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new PhysicianEditDialog(App.Db) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true)
            ViewModel.LoadPhysiciansCommand.Execute(null);
    }

    private void EditPhysician_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Physician physician)
        {
            var dlg = new PhysicianEditDialog(App.Db, physician) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() == true)
                ViewModel.LoadPhysiciansCommand.Execute(null);
        }
    }
}
