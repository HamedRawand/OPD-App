using System.IO;
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
    private CustomRolesView?          _customRolesView;
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
        UserRoleText.Text = App.Auth.CurrentRoleDisplayName;

        var v = Assembly.GetExecutingAssembly().GetName().Version;
        AppVersionText.Text = v is null ? "" : $"v{v.Major}.{v.Minor}.{v.Build}";

        // Each nav item shown only when the user holds the required permission.
        // This ensures custom role users see exactly what they are permitted to access.
        if (App.Auth.Can(Permission.ViewPatients))
            NavPatientsBtn.Visibility = Visibility.Visible;

        if (App.Auth.Can(Permission.ManagePhysicians))
            NavPhysiciansBtn.Visibility = Visibility.Visible;

        if (App.Auth.Can(Permission.ManageMedicineCatalog))
            NavMedicinesBtn.Visibility = Visibility.Visible;

        // ADMINISTRATION section: Users, Custom Roles, Import, Backup, Options, Print Settings
        if (App.Auth.Can(Permission.ManageUsers))
            AdminNav.Visibility = Visibility.Visible;

        _dashboardView   = new DashboardView();
        _patientListView = new PatientListView();

        // Landing page: Dashboard if user can view patients; otherwise first permitted section
        if (App.Auth.Can(Permission.ViewPatients))
        {
            ContentArea.Content = _dashboardView;
            _dashboardView.Reload();
            SetActiveNav(NavDashboardBtn);
        }
        else if (App.Auth.Can(Permission.ManageMedicineCatalog))
        {
            _catalogView = new CatalogView();
            ContentArea.Content = _catalogView;
            _catalogView.ViewModel.LoadMedicinesCommand.Execute(null);
            SetActiveNav(NavMedicinesBtn);
        }
        else if (App.Auth.Can(Permission.ManagePhysicians))
        {
            _physicianView = new PhysicianView();
            ContentArea.Content = _physicianView;
            _physicianView.ViewModel.LoadPhysiciansCommand.Execute(null);
            SetActiveNav(NavPhysiciansBtn);
        }
        else
        {
            // Fallback — show dashboard (edge case: no recognized permissions)
            ContentArea.Content = _dashboardView;
            _dashboardView.Reload();
            SetActiveNav(NavDashboardBtn);
        }

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

        // Set initial sidebar width proportional to window width
        UpdateSidebarWidth(Width);
    }

    // ── Responsive sidebar ────────────────────────────────────────────────────

    /// <summary>
    /// Sidebar takes ~19 % of the window width, clamped between 175 px (min)
    /// and 240 px (max) so it never looks too narrow on 800 px windows or too
    /// wide on large monitors / low-DPI screens.
    /// </summary>
    private void UpdateSidebarWidth(double windowWidth)
    {
        var w = Math.Clamp(windowWidth * 0.19, 175, 240);
        SidebarColumn.Width = new GridLength(w);
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        => UpdateSidebarWidth(e.NewSize.Width);

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
        UserRoleText.Text = App.Auth.CurrentRoleDisplayName;
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

    private void NavCustomRoles_Click(object sender, RoutedEventArgs e)
    {
        SetActiveNav((Button)sender);
        _customRolesView ??= new CustomRolesView();
        ContentArea.Content = _customRolesView;
        // Refresh the list whenever the user navigates back to this view
        if (_customRolesView.DataContext is ViewModels.CustomRolesViewModel vm)
            vm.LoadRolesCommand.Execute(null);
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

    /// <summary>
    /// Called by <see cref="DashboardView"/> "Create Backup" so the user lands on the
    /// full Backup &amp; Restore page where they can set an encryption password.
    /// </summary>
    public void NavigateToBackup()
    {
        SetActiveNav(NavBackupBtn);
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

    private void NavHelp_Click(object sender, RoutedEventArgs e)
    {
        var manualPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "Help", "RxWriter_UserManual.html");

        if (!File.Exists(manualPath))
        {
            MessageBox.Show(
                "The user manual could not be found.\n\n" +
                "Please contact support at info.rxwriter@gmail.com",
                "Manual Not Found",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName        = manualPath,
            UseShellExecute = true
        });
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

    private void NavAbout_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new AboutDialog { Owner = this };
        dlg.ShowDialog();
    }

    private void ContactEmail_Click(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName        = "mailto:info.rxwriter@gmail.com",
            UseShellExecute = true
        });
    }
}
