using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
        ViewModel = new PatientListViewModel(App.DbFactory);
        DataContext = ViewModel;
    }

    /// <summary>Opens the edit window for a brand-new patient (+ their first visit).</summary>
    private void NewPatient_Click(object sender, RoutedEventArgs e)
    {
        if (!App.Auth.Can(Services.Permission.RegisterPatient)) return;
        var vm  = new PatientEditViewModel(App.DbFactory);
        var win = new PatientEditWindow(vm) { Owner = Window.GetWindow(this) };
        if (win.ShowDialog() == true)
            ViewModel.LoadPatientsCommand.Execute(null);
    }

    /// <summary>Opens PatientDetailView (visit history) for the selected patient row.</summary>
    private void ViewPatient_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is PatientListRow row)
        {
            AuditService.Log("PatientViewed", "Patient", row.Patient.Id, row.Patient.PatientName);
            var win = new PatientDetailWindow(row.Patient)
            {
                Owner = Window.GetWindow(this)
            };
            win.ShowDialog();
            // Reload in case visits were added/edited/deleted
            ViewModel.LoadPatientsCommand.Execute(null);
        }
    }

    private void PatientsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        PatientsGrid.UnselectAll();
    }

    // ── Keyboard shortcuts ────────────────────────────────────────────────────
    // Ctrl+F  →  focus the search box and select-all
    // Ctrl+N  →  open New Patient (same as clicking the button)

    private void UserControl_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Control) == 0) return;

        if (e.Key == Key.F)
        {
            SearchBox.Focus();
            SearchBox.SelectAll();
            e.Handled = true;
        }
        else if (e.Key == Key.N)
        {
            NewPatient_Click(this, new RoutedEventArgs());
            e.Handled = true;
        }
    }
}
