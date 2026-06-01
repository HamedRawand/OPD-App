using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using OPDClinic.Models;
using OPDClinic.Services;
using OPDClinic.ViewModels;
using Serilog;

namespace OPDClinic.Views;

public partial class PatientDetailWindow : Window
{
    private readonly PatientDetailViewModel _vm;

    public PatientDetailWindow(Patient patient)
    {
        InitializeComponent();
        _vm         = new PatientDetailViewModel(App.DbFactory, patient);
        DataContext = _vm;

        // Ctrl+N → New Visit
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.N && (Keyboard.Modifiers & ModifierKeys.Control) != 0)
            {
                NewVisit_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
        };
    }

    // ── New visit ─────────────────────────────────────────────────────────────

    private void NewVisit_Click(object sender, RoutedEventArgs e)
    {
        if (!App.Auth.Can(Permission.RegisterPatient)) return;
        var vm  = new PatientEditViewModel(App.DbFactory, _vm.Patient);
        var win = new PatientEditWindow(vm) { Owner = this };
        if (win.ShowDialog() == true)
            _vm.LoadVisitsCommand.Execute(null);
    }

    // ── Edit existing visit ───────────────────────────────────────────────────

    private void EditVisit_Click(object sender, RoutedEventArgs e)
    {
        if (!App.Auth.Can(Permission.EnterClinicalData)) return;
        if (sender is not Button btn || btn.Tag is not VisitListRow row) return;

        var vm  = new PatientEditViewModel(App.DbFactory, _vm.Patient, row.Visit);
        var win = new PatientEditWindow(vm) { Owner = this };
        if (win.ShowDialog() == true)
            _vm.LoadVisitsCommand.Execute(null);
    }

    // ── Repeat prescription ───────────────────────────────────────────────────

    private void RepeatVisit_Click(object sender, RoutedEventArgs e)
    {
        if (!App.Auth.Can(Permission.RegisterPatient)) return;
        if (sender is not Button btn || btn.Tag is not VisitListRow row) return;

        // Load the source visit from the database (we only have a snapshot in the row)
        using var db = App.DbFactory.CreateDbContext();
        var sourceVisit = db.Visits.Find(row.VisitId);
        if (sourceVisit is null) return;

        var vm = new PatientEditViewModel(App.DbFactory, _vm.Patient);
        vm.RepeatFromVisit(sourceVisit);

        var win = new PatientEditWindow(vm) { Owner = this };
        if (win.ShowDialog() == true)
            _vm.LoadVisitsCommand.Execute(null);
    }

    // ── Print visit ───────────────────────────────────────────────────────────

    private void PrintVisit_Click(object sender, RoutedEventArgs e)
    {
        if (!App.Auth.Can(Permission.PrintPdf)) return;
        if (sender is not Button btn || btn.Tag is not VisitListRow row) return;

        try
        {
            var path = new PdfService(App.DbFactory).GenerateForVisit(row.VisitId);
            PrintService.OpenPdf(path);
            AuditService.Log("PrescriptionPrinted", "Visit", row.VisitId, _vm.Patient.PatientName);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "PDF generation failed for VisitId={VisitId}", row.VisitId);
            MessageBox.Show($"PDF generation failed:\n{ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ── Delete visit ──────────────────────────────────────────────────────────

    private void DeleteVisit_Click(object sender, RoutedEventArgs e)
    {
        if (!App.Auth.Can(Permission.EnterClinicalData)) return;
        if (sender is not Button btn || btn.Tag is not VisitListRow row) return;

        var result = MessageBox.Show(
            $"Delete visit from {row.DateText}?\n\n" +
            "All prescription lines and lab tests for this visit will also be deleted.\n" +
            "This cannot be undone.",
            "Delete Visit",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            using var db = App.DbFactory.CreateDbContext();
            var visit = db.Visits.Find(row.VisitId);
            if (visit is not null)
            {
                db.Visits.Remove(visit);
                db.SaveChanges();
                AuditService.Log("VisitDeleted", "Visit", row.VisitId, _vm.Patient.PatientName);
                Log.Information("VisitDeleted — VisitId:{Id} Patient:{Name}", row.VisitId, _vm.Patient.PatientName);
            }
            _vm.LoadVisitsCommand.Execute(null);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Delete visit failed for VisitId={VisitId}", row.VisitId);
            MessageBox.Show($"Delete failed:\n{ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ── Timeline expand / collapse ────────────────────────────────────────────

    private void ToggleDetails_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is VisitListRow row)
            row.IsExpanded = !row.IsExpanded;
    }
}
