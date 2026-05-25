using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using OPDClinic.Models;
using OPDClinic.Services;
using OPDClinic.Views;
using Serilog;

namespace OPDClinic;

public partial class MainWindow : Window
{
    private readonly DashboardView    _dashboardView;
    private readonly PatientListView  _patientListView;
    private PhysicianView?            _physicianView;
    private CatalogView?              _catalogView;
    private UserManagementView?       _userMgmtView;
    private ImportWizardView?         _importView;
    private BackupView?               _backupView;
    private OptionsView?              _optionsView;
    private ReportDesignView?         _settingsView;

    private readonly DispatcherTimer _idleTimer;
    private DateTime _lastActivity = DateTime.UtcNow;
    private const int IdleMinutes = 10;

    private Button? _activeNavBtn;

    public MainWindow()
    {
        InitializeComponent();

        var user = App.Auth.CurrentUser!;
        UserNameText.Text = user.FullName;
        UserRoleText.Text = user.Role.ToString();

        var v = Assembly.GetExecutingAssembly().GetName().Version;
        AppVersionText.Text = v is null ? "" : $"v{v.Major}.{v.Minor}.{v.Build}";

        if (user.Role == UserRole.Admin)
            AdminNav.Visibility = Visibility.Visible;

        _dashboardView   = new DashboardView();
        _patientListView = new PatientListView();

        // Dashboard is the default landing page
        ContentArea.Content = _dashboardView;
        _dashboardView.Reload();
        SetActiveNav(NavDashboardBtn);

        // Language change listener
        LanguageService.LanguageChanged += OnLanguageChanged;

        // Idle auto-lock timer
        _idleTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(IdleMinutes) };
        _idleTimer.Tick += (_, _) => LockScreen();
        _idleTimer.Start();
        InputManager.Current.PreProcessInput += OnUserActivity;

        Closing += OnWindowClosing;

        // Fire background update check (non-blocking, 24h cooldown)
        _ = CheckForUpdatesInBackground();
    }

    private void OnWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        LanguageService.LanguageChanged -= OnLanguageChanged;
        InputManager.Current.PreProcessInput -= OnUserActivity;
        _idleTimer.Stop();
    }

    // ── Idle timer ────────────────────────────────────────────────────────────

    private void OnUserActivity(object sender, PreProcessInputEventArgs e)
    {
        if (LockOverlay.Visibility == Visibility.Visible) return;
        var now = DateTime.UtcNow;
        if ((now - _lastActivity).TotalSeconds > 10)
        {
            _lastActivity = now;
            _idleTimer.Stop();
            _idleTimer.Start();
        }
    }

    private void LockScreen()
    {
        _idleTimer.Stop();
        LockUserText.Text = App.Auth.CurrentUser?.FullName ?? "";
        LockPasswordBox.Password = "";
        LockErrorText.Visibility = Visibility.Collapsed;
        LockOverlay.Visibility = Visibility.Visible;
        LockPasswordBox.Focus();

        // Disable all child windows so PatientEditWindow etc. are inaccessible
        foreach (Window w in Application.Current.Windows)
            if (w != this) w.IsEnabled = false;

        AuditService.Log("SessionAutoLocked");
    }

    private void Unlock_Click(object sender, RoutedEventArgs e)
    {
        if (!App.Auth.VerifyCurrentPassword(LockPasswordBox.Password))
        {
            LockErrorText.Visibility = Visibility.Visible;
            LockPasswordBox.Clear();
            LockPasswordBox.Focus();
            return;
        }
        LockOverlay.Visibility = Visibility.Collapsed;
        _lastActivity = DateTime.UtcNow;
        _idleTimer.Start();

        // Re-enable all child windows
        foreach (Window w in Application.Current.Windows)
            if (w != this) w.IsEnabled = true;

        AuditService.Log("SessionUnlocked");
    }

    private void LockPasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            Unlock_Click(sender, e);
    }

    // ── Language ──────────────────────────────────────────────────────────────

    private void OnLanguageChanged()
    {
        UserRoleText.Text = App.Auth.CurrentUser!.Role.ToString();
    }

    private void LangToggle_Click(object sender, RoutedEventArgs e)
        => LanguageService.Toggle();

    // ── Navigation ────────────────────────────────────────────────────────────

    private void SetActiveNav(Button btn)
    {
        if (_activeNavBtn is not null)
            _activeNavBtn.Style = (Style)FindResource("NavButton");
        btn.Style = (Style)FindResource("NavButtonActive");
        _activeNavBtn = btn;
    }

    private void NavDashboard_Click(object sender, RoutedEventArgs e)
    {
        SetActiveNav((Button)sender);
        ContentArea.Content = _dashboardView;
        _dashboardView.Reload();
    }

    private void NavPatients_Click(object sender, RoutedEventArgs e)
    {
        SetActiveNav((Button)sender);
        ContentArea.Content = _patientListView;
        _patientListView.ViewModel.LoadPatientsCommand.Execute(null);
    }

    private void NavPhysicians_Click(object sender, RoutedEventArgs e)
    {
        SetActiveNav((Button)sender);
        _physicianView ??= new PhysicianView();
        ContentArea.Content = _physicianView;
        _physicianView.ViewModel.LoadPhysiciansCommand.Execute(null);
    }

    private void NavMedicines_Click(object sender, RoutedEventArgs e)
    {
        SetActiveNav((Button)sender);
        _catalogView ??= new CatalogView();
        ContentArea.Content = _catalogView;
        _catalogView.ViewModel.LoadMedicinesCommand.Execute(null);
    }

    private void NavUsers_Click(object sender, RoutedEventArgs e)
    {
        SetActiveNav((Button)sender);
        _userMgmtView ??= new UserManagementView();
        ContentArea.Content = _userMgmtView;
        _userMgmtView.ViewModel.LoadUsersCommand.Execute(null);
    }

    private void NavImport_Click(object sender, RoutedEventArgs e)
    {
        SetActiveNav((Button)sender);
        _importView ??= new ImportWizardView();
        ContentArea.Content = _importView;
    }

    private void NavBackup_Click(object sender, RoutedEventArgs e)
    {
        SetActiveNav((Button)sender);
        _backupView ??= new BackupView();
        ContentArea.Content = _backupView;
    }

    private void NavOptions_Click(object sender, RoutedEventArgs e)
    {
        SetActiveNav((Button)sender);
        _optionsView ??= new OptionsView();
        ContentArea.Content = _optionsView;
    }

    private void NavSettings_Click(object sender, RoutedEventArgs e)
    {
        SetActiveNav((Button)sender);
        _settingsView ??= new ReportDesignView();
        ContentArea.Content = _settingsView;
    }

    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        AuditService.Log("UserLogout");
        App.Auth.Logout();
        new LoginWindow().Show();
        Close();
    }

    // ── Auto-update ───────────────────────────────────────────────────────────

    /// <summary>
    /// Startup background check. Respects the 24-hour cooldown so the API is not
    /// hit on every launch. Shows the update dialog on the UI thread if a newer
    /// version is available.
    /// </summary>
    private async Task CheckForUpdatesInBackground()
    {
        if (UpdateService.IsWithinCooldown()) return;

        var info = await UpdateService.CheckForUpdateAsync();
        if (info is null) return;

        ShowUpdateDialog(info);
    }

    /// <summary>Manual check — always runs, ignores cooldown.</summary>
    private async void NavUpdates_Click(object sender, RoutedEventArgs e)
    {
        NavUpdatesBtn.IsEnabled = false;
        try
        {
            var info = await UpdateService.CheckForUpdateAsync();
            if (info is not null)
            {
                ShowUpdateDialog(info);
            }
            else
            {
                MessageBox.Show(
                    (string)FindResource("Update.UpToDate"),
                    (string)FindResource("Update.Title"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Manual update check failed");
            MessageBox.Show(
                (string)FindResource("Update.CheckFailed"),
                (string)FindResource("Update.Title"),
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        finally
        {
            NavUpdatesBtn.IsEnabled = true;
        }
    }

    private void ShowUpdateDialog(UpdateInfo info)
    {
        var dlg = new UpdateAvailableDialog(info) { Owner = this };
        dlg.ShowDialog();
    }
}
