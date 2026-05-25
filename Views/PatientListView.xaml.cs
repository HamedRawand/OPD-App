using System.Windows;
using System.Windows.Controls;
using OPDClinic.Models;
using OPDClinic.Services;
using OPDClinic.ViewModels;

namespace OPDClinic.Views;

public partial class PatientListView : UserControl
{
    public PatientListViewModel ViewModel { get; }

    public PatientListView()
    {
        InitializeComponent();
        ViewModel = new PatientListViewModel(App.Db);
        DataContext = ViewModel;
    }

    private void NewVisit_Click(object sender, RoutedEventArgs e)
    {
        if (!App.Auth.Can(Services.Permission.CreateEditPatient)) return;
        var vm  = new PatientEditViewModel(App.Db);
        var win = new PatientEditWindow(vm) { Owner = Window.GetWindow(this) };
        if (win.ShowDialog() == true)
            ViewModel.LoadPatientsCommand.Execute(null);
    }

    private void EditVisit_Click(object sender, RoutedEventArgs e)
    {
        if (!App.Auth.Can(Services.Permission.CreateEditPatient)) return;
        if (sender is Button btn && btn.Tag is Patient patient)
        {
            var vm  = new PatientEditViewModel(App.Db, patient);
            var win = new PatientEditWindow(vm) { Owner = Window.GetWindow(this) };
            if (win.ShowDialog() == true)
                ViewModel.LoadPatientsCommand.Execute(null);
        }
    }

    private void PrintVisit_Click(object sender, RoutedEventArgs e)
    {
        if (!App.Auth.Can(Permission.PrintPdf)) return;
        if (sender is Button btn && btn.Tag is Patient patient)
        {
            try
            {
                var path = new PdfService(App.Db).GenerateForPatient(patient.Id);
                PrintService.OpenPdf(path);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"PDF generation failed:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void PatientsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        PatientsGrid.UnselectAll();
    }
}
