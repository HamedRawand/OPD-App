using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using OPDClinic.Services;
using Serilog;

namespace OPDClinic.Views;

public partial class ImportWizardView : UserControl
{
    private readonly MigrationService _migration = new(App.Db);

    public ImportWizardView()
    {
        InitializeComponent();
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Select Access Database",
            Filter = "Access Database (*.accdb;*.mdb)|*.accdb;*.mdb",
            CheckFileExists = true
        };

        if (dlg.ShowDialog() != true) return;

        FilePathBox.Text = dlg.FileName;
        ImportBtn.IsEnabled = true;
        ResultCard.Visibility = Visibility.Collapsed;
    }

    private async void Import_Click(object sender, RoutedEventArgs e)
    {
        ImportBtn.IsEnabled = false;
        ResultCard.Visibility = Visibility.Collapsed;

        var path = FilePathBox.Text;
        MigrationResult result = null!;

        await System.Threading.Tasks.Task.Run(() =>
        {
            result = _migration.Import(path);
        });

        ResultCard.Visibility = Visibility.Visible;

        if (result.Success)
        {
            AuditService.Log("ImportCompleted", details:
                $"Physicians={result.Physicians} LabTests={result.LabTests} " +
                $"Medicines={result.Medicines} Patients={result.Patients} " +
                $"Prescriptions={result.Prescriptions}");
            Log.Information(
                "Access import succeeded — Physicians:{P} LabTests:{L} Medicines:{M} Patients:{Pt} Prescriptions:{Rx}",
                result.Physicians, result.LabTests, result.Medicines, result.Patients, result.Prescriptions);

            ResultTitle.Text = "Import completed successfully.";
            ResultTitle.Foreground = System.Windows.Media.Brushes.DarkGreen;

            PhysiciansCount.Text   = result.Physicians == 0    ? "Already imported" : $"{result.Physicians} imported";
            LabTestsCount.Text     = result.LabTests == 0      ? "Already imported" : $"{result.LabTests} imported";
            MedicinesCount.Text    = result.Medicines == 0     ? "Already imported" : $"{result.Medicines} imported";
            PatientsCount.Text     = result.Patients == 0      ? "Already imported" : $"{result.Patients} imported";
            PrescriptionsCount.Text= result.Prescriptions == 0 ? "Already imported" : $"{result.Prescriptions} imported";

            CountGrid.Visibility  = Visibility.Visible;
            ErrorDetail.Visibility = Visibility.Collapsed;
        }
        else
        {
            Log.Warning("Access import failed: {Error}", result.Error);

            ResultTitle.Text = "Import failed.";
            ResultTitle.Foreground = System.Windows.Media.Brushes.DarkRed;
            CountGrid.Visibility   = Visibility.Collapsed;
            ErrorDetail.Text       = result.Error;
            ErrorDetail.Visibility = Visibility.Visible;
        }

        ImportBtn.IsEnabled = true;
    }
}
