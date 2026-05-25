using System.Windows;
using System.Windows.Controls;
using OPDClinic.Models;
using OPDClinic.ViewModels;

namespace OPDClinic.Views;

public partial class OptionsView : UserControl
{
    public OptionsViewModel ViewModel { get; }

    private Window Owner => Window.GetWindow(this) ?? Application.Current.MainWindow;

    public OptionsView()
    {
        InitializeComponent();
        ViewModel = new OptionsViewModel(App.Db);
        DataContext = ViewModel;
    }

    // ── Route of Administration ───────────────────────────────────────────────

    private void AddRoute_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new RouteEditDialog(App.Db, null) { Owner = Owner };
        if (dlg.ShowDialog() == true)
            ViewModel.LoadRoutesCommand.Execute(null);
    }

    private void EditRoute_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is RouteOfAdministration item)
        {
            var dlg = new RouteEditDialog(App.Db, item) { Owner = Owner };
            if (dlg.ShowDialog() == true)
                ViewModel.LoadRoutesCommand.Execute(null);
        }
    }

    // ── Medicine Categories ───────────────────────────────────────────────────

    private void AddMedicineCategory_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new MedicineCategoryEditDialog(App.Db, null) { Owner = Owner };
        if (dlg.ShowDialog() == true)
            ViewModel.LoadMedicineCategoriesCommand.Execute(null);
    }

    private void EditMedicineCategory_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is MedicineForm item)
        {
            var dlg = new MedicineCategoryEditDialog(App.Db, item) { Owner = Owner };
            if (dlg.ShowDialog() == true)
                ViewModel.LoadMedicineCategoriesCommand.Execute(null);
        }
    }

    // ── Dosage ────────────────────────────────────────────────────────────────

    private void AddDosage_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new DosageEditDialog(App.Db, null) { Owner = Owner };
        if (dlg.ShowDialog() == true)
            ViewModel.LoadDosagesCommand.Execute(null);
    }

    private void EditDosage_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is Dosage item)
        {
            var dlg = new DosageEditDialog(App.Db, item) { Owner = Owner };
            if (dlg.ShowDialog() == true)
                ViewModel.LoadDosagesCommand.Execute(null);
        }
    }

    // ── Medicine Notes ────────────────────────────────────────────────────────

    private void AddMedicineNote_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new MedicineNoteEditDialog(App.Db, null) { Owner = Owner };
        if (dlg.ShowDialog() == true)
            ViewModel.LoadMedicineNotesCommand.Execute(null);
    }

    private void EditMedicineNote_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is MedicineNote item)
        {
            var dlg = new MedicineNoteEditDialog(App.Db, item) { Owner = Owner };
            if (dlg.ShowDialog() == true)
                ViewModel.LoadMedicineNotesCommand.Execute(null);
        }
    }

    // ── Prescription Notes ────────────────────────────────────────────────────

    private void AddPrescriptionNote_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new PrescriptionNoteEditDialog(App.Db, null) { Owner = Owner };
        if (dlg.ShowDialog() == true)
            ViewModel.LoadPrescriptionNotesCommand.Execute(null);
    }

    private void EditPrescriptionNote_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is PrescriptionNote item)
        {
            var dlg = new PrescriptionNoteEditDialog(App.Db, item) { Owner = Owner };
            if (dlg.ShowDialog() == true)
                ViewModel.LoadPrescriptionNotesCommand.Execute(null);
        }
    }

    // ── Lab Tests ─────────────────────────────────────────────────────────────

    private void AddLabTest_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new LabTestEditDialog(App.Db, null) { Owner = Owner };
        if (dlg.ShowDialog() == true)
            ViewModel.LoadLabTestsCommand.Execute(null);
    }

    private void EditLabTest_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is LabTest item)
        {
            var dlg = new LabTestEditDialog(App.Db, item) { Owner = Owner };
            if (dlg.ShowDialog() == true)
                ViewModel.LoadLabTestsCommand.Execute(null);
        }
    }
}
