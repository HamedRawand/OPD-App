using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using OPDClinic.Helpers;
using OPDClinic.Services;
using OPDClinic.ViewModels;

namespace OPDClinic.Views;

public partial class PatientEditWindow : Window
{
    private readonly PatientEditViewModel _vm;
    private readonly PropertyChangedEventHandler _vmHandler;
    private bool _printAfterSave;

    public PatientEditWindow(PatientEditViewModel vm)
    {
        InitializeComponent();
        DialogHelper.ApplyConstraints(this);
        _vm = vm;
        DataContext = vm;

        _vmHandler = (_, e) =>
        {
            if (e.PropertyName != nameof(PatientEditViewModel.IsSaved) || !vm.IsSaved) return;

            if (_printAfterSave)
                GenerateAndOpenPdf();

            DialogResult = true;
            Close();
        };

        vm.PropertyChanged += _vmHandler;
        Closing += OnWindowClosing;
    }

    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        // Warn if the user has started entering data but hasn't saved yet
        if (!_vm.IsSaved && !string.IsNullOrWhiteSpace(_vm.PatientName))
        {
            var result = MessageBox.Show(
                "You have unsaved changes. Close without saving?",
                "Unsaved Changes", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.No)
            {
                e.Cancel = true;
                return;
            }
        }
        _vm.PropertyChanged -= _vmHandler;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void ApplyTemplate_Click(object sender, RoutedEventArgs e)
    {
        var rx = _vm.Prescription;
        if (rx.Lines.Count > 0 || rx.SelectedLabTests.Count > 0 || rx.SelectedPrescriptionNote is not null)
        {
            MessageBox.Show(
                "Applying a template is only available when the prescription is empty. " +
                "Clear the existing lines, lab tests and footer note first, or start a new visit.",
                "Prescription Not Empty", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var picker = new PrescriptionTemplatePickerDialog(App.DbFactory) { Owner = this };
        if (picker.ShowDialog() != true || picker.SelectedTemplate is null) return;

        _vm.ApplyTemplate(picker.SelectedTemplate);
    }

    private void SaveAndPrint_Click(object sender, RoutedEventArgs e)
    {
        _printAfterSave = true;
        _vm.SaveCommand.Execute(null);
        // If save fails (validation error), IsSaved stays false — reset flag
        if (!_vm.IsSaved) _printAfterSave = false;
    }

    // ── Keyboard shortcuts ────────────────────────────────────────────────────
    // Ctrl+S → Save,  Ctrl+P → Save & Print

    private void SaveCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        => _vm.SaveCommand.Execute(null);

    private void PrintCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        => SaveAndPrint_Click(sender, new RoutedEventArgs());

    // ─────────────────────────────────────────────────────────────────────────

    private void GenerateAndOpenPdf()
    {
        try
        {
            var path = new PdfService(App.DbFactory).GenerateForVisit(_vm.SavedVisitId);
            PrintService.OpenPdf(path);
            AuditService.Log("PrescriptionPrinted", "Visit", _vm.SavedVisitId, _vm.PatientName);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"PDF generation failed: {ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
