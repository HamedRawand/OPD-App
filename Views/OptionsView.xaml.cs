using System.Windows;
using System.Windows.Controls;
using OPDClinic.Models;
using OPDClinic.Services;
using OPDClinic.ViewModels;

namespace OPDClinic.Views;

public partial class OptionsView : UserControl
{
    public OptionsViewModel ViewModel { get; }

    private Window Owner => Window.GetWindow(this) ?? Application.Current.MainWindow;

    public OptionsView()
    {
        InitializeComponent();
        ViewModel = new OptionsViewModel(App.DbFactory);
        DataContext = ViewModel;
    }

    // ── Route of Administration ───────────────────────────────────────────────

    private void AddRoute_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new RouteEditDialog(App.DbFactory, null) { Owner = Owner };
        if (dlg.ShowDialog() == true)
            ViewModel.LoadRoutesCommand.Execute(null);
    }

    private void EditRoute_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is RouteOfAdministration item)
        {
            var dlg = new RouteEditDialog(App.DbFactory, item) { Owner = Owner };
            if (dlg.ShowDialog() == true)
                ViewModel.LoadRoutesCommand.Execute(null);
        }
    }

    // ── Medicine Categories ───────────────────────────────────────────────────

    private void AddMedicineCategory_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new MedicineCategoryEditDialog(App.DbFactory, null) { Owner = Owner };
        if (dlg.ShowDialog() == true)
            ViewModel.LoadMedicineCategoriesCommand.Execute(null);
    }

    private void EditMedicineCategory_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is MedicineForm item)
        {
            var dlg = new MedicineCategoryEditDialog(App.DbFactory, item) { Owner = Owner };
            if (dlg.ShowDialog() == true)
                ViewModel.LoadMedicineCategoriesCommand.Execute(null);
        }
    }

    // ── Dosage ────────────────────────────────────────────────────────────────

    private void AddDosage_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new DosageEditDialog(App.DbFactory, null) { Owner = Owner };
        if (dlg.ShowDialog() == true)
            ViewModel.LoadDosagesCommand.Execute(null);
    }

    private void EditDosage_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is Dosage item)
        {
            var dlg = new DosageEditDialog(App.DbFactory, item) { Owner = Owner };
            if (dlg.ShowDialog() == true)
                ViewModel.LoadDosagesCommand.Execute(null);
        }
    }

    // ── Medicine Notes ────────────────────────────────────────────────────────

    private void AddMedicineNote_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new MedicineNoteEditDialog(App.DbFactory, null) { Owner = Owner };
        if (dlg.ShowDialog() == true)
            ViewModel.LoadMedicineNotesCommand.Execute(null);
    }

    private void EditMedicineNote_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is MedicineNote item)
        {
            var dlg = new MedicineNoteEditDialog(App.DbFactory, item) { Owner = Owner };
            if (dlg.ShowDialog() == true)
                ViewModel.LoadMedicineNotesCommand.Execute(null);
        }
    }

    // ── Prescription Notes ────────────────────────────────────────────────────

    private void AddPrescriptionNote_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new PrescriptionNoteEditDialog(App.DbFactory, null) { Owner = Owner };
        if (dlg.ShowDialog() == true)
            ViewModel.LoadPrescriptionNotesCommand.Execute(null);
    }

    private void EditPrescriptionNote_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is PrescriptionNote item)
        {
            var dlg = new PrescriptionNoteEditDialog(App.DbFactory, item) { Owner = Owner };
            if (dlg.ShowDialog() == true)
                ViewModel.LoadPrescriptionNotesCommand.Execute(null);
        }
    }

    // ── Export handlers ───────────────────────────────────────────────────────

    private void ExportRoutes_Click(object sender, RoutedEventArgs e)
        => ExportService.ExportRoutes(ViewModel.Routes);

    private void ExportMedicineCategories_Click(object sender, RoutedEventArgs e)
        => ExportService.ExportMedicineCategories(ViewModel.MedicineCategories);

    private void ExportDosages_Click(object sender, RoutedEventArgs e)
        => ExportService.ExportDosages(ViewModel.Dosages);

    private void ExportMedicineNotes_Click(object sender, RoutedEventArgs e)
        => ExportService.ExportMedicineNotes(ViewModel.MedicineNotes);

    private void ExportPrescriptionNotes_Click(object sender, RoutedEventArgs e)
        => ExportService.ExportPrescriptionNotes(ViewModel.PrescriptionNotes);

    private void ExportLabTests_Click(object sender, RoutedEventArgs e)
        => ExportService.ExportLabTests(ViewModel.LabTests);

    // ── Lab Tests ─────────────────────────────────────────────────────────────

    private void AddLabTest_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new LabTestEditDialog(App.DbFactory, null) { Owner = Owner };
        if (dlg.ShowDialog() == true)
            ViewModel.LoadLabTestsCommand.Execute(null);
    }

    private void EditLabTest_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is LabTest item)
        {
            var dlg = new LabTestEditDialog(App.DbFactory, item) { Owner = Owner };
            if (dlg.ShowDialog() == true)
                ViewModel.LoadLabTestsCommand.Execute(null);
        }
    }
}
