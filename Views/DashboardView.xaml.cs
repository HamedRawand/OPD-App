using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using OPDClinic.ViewModels;
using OPDClinic.Services;

namespace OPDClinic.Views;

public partial class DashboardView : UserControl
{
    public DashboardViewModel ViewModel { get; } = new();

    public DashboardView()
    {
        InitializeComponent();
        DataContext = ViewModel;
    }

    /// <summary>Called by MainWindow when the user navigates to the Dashboard.</summary>
    public void Reload()
    {
        ViewModel.Load();
        BindStats();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// Push ViewModel values into the named TextBlocks (simpler than converters).
    private void BindStats()
    {
        TotalPatientsText.Text   = ViewModel.TotalPatients.ToString("N0");
        TodayPatientsText.Text   = ViewModel.TodayVisits.ToString("N0");
        TotalMedicinesText.Text  = ViewModel.TotalMedicines.ToString("N0");
        TotalPhysiciansText.Text = ViewModel.TotalPhysicians.ToString("N0");

        TodayGregorianText.Text  = ViewModel.TodayDateText;
        TodayShamsiLabel.Text    = ViewModel.TodayShamsiText;

        LastBackupDateText.Text  = ViewModel.LastBackupText;
        LastBackupSizeText.Text  = ViewModel.LastBackupSubText;

        DbSizeText.Text          = ViewModel.DatabaseSizeText;

        // Recent visits grid
        RecentGrid.ItemsSource   = ViewModel.RecentVisits;
        NoVisitsText.Visibility  = ViewModel.RecentVisits.Count == 0
            ? Visibility.Visible : Visibility.Collapsed;
        RecentGrid.Visibility    = ViewModel.RecentVisits.Count > 0
            ? Visibility.Visible : Visibility.Collapsed;

        ShowStatus();
    }

    private void ShowStatus()
    {
        if (string.IsNullOrEmpty(ViewModel.StatusMessage))
        {
            StatusBorder.Visibility = Visibility.Collapsed;
            return;
        }
        StatusText.Text             = ViewModel.StatusMessage;
        StatusBorder.Background     = ViewModel.StatusIsError
            ? new SolidColorBrush(Color.FromRgb(0xFE, 0xE2, 0xE2))
            : new SolidColorBrush(Color.FromRgb(0xDC, 0xFC, 0xE7));
        StatusText.Foreground       = ViewModel.StatusIsError
            ? new SolidColorBrush(Color.FromRgb(0xB9, 0x1C, 0x1C))
            : new SolidColorBrush(Color.FromRgb(0x16, 0x65, 0x34));
        StatusBorder.Visibility     = Visibility.Visible;
    }

    // ── Event handlers ────────────────────────────────────────────────────────

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.StatusMessage = "";
        Reload();
    }

    private void NewVisit_Click(object sender, RoutedEventArgs e)
    {
        if (!App.Auth.Can(Services.Permission.RegisterPatient)) return;

        var vm  = new PatientEditViewModel(App.DbFactory);
        var win = new PatientEditWindow(vm) { Owner = Window.GetWindow(this) };
        if (win.ShowDialog() == true)
            Reload();   // refresh today's count
    }

    private void CreateBackup_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.CreateBackup();
        BindStats();
    }

    private void OpenPatient_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not RecentVisitRow row) return;

        using var db = App.DbFactory.CreateDbContext();
        var patient = db.Patients.Find(row.PatientId);
        if (patient is null) return;

        var win = new PatientDetailWindow(patient) { Owner = Window.GetWindow(this) };
        win.ShowDialog();
        Reload();   // refresh stats in case a visit was added
    }
}
