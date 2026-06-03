using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using OPDClinic.Data;
using OPDClinic.Services;
using Serilog;

namespace OPDClinic.Views;

public partial class BackupView : UserControl
{
    private string _backupFolder = BackupService.DefaultBackupFolder;

    public BackupView()
    {
        InitializeComponent();
        BackupFolderBox.Text = _backupFolder;
        RefreshBackupList();

        // Merge from Another Clinic is restricted to full Admin only (not Co-Admin)
        if (!App.Auth.IsFullAdmin)
            MergeCard.Visibility = System.Windows.Visibility.Collapsed;
    }

    // ── Backup ────────────────────────────────────────────────────────────────

    private void ChangeFolder_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFolderDialog
        {
            Title = "Select backup folder",
            InitialDirectory = _backupFolder
        };
        if (dlg.ShowDialog() != true) return;

        _backupFolder = dlg.FolderName;
        BackupFolderBox.Text = _backupFolder;
        RefreshBackupList();
    }

    private void BackupPassword_Changed(object sender, RoutedEventArgs e)
    {
        var hasPassword = BackupPasswordBox.Password.Length > 0;
        EncryptionHint.Text = hasPassword
            ? "🔒 Backup will be AES-256 encrypted (.rxb). Keep this password safe — you need it to restore."
            : "⚠  No password — backup will be unencrypted (.zip). Anyone with the file can read patient data.";
        EncryptionHint.Foreground = new SolidColorBrush(hasPassword
            ? Color.FromRgb(21, 101, 192)    // blue — encrypted
            : Color.FromRgb(146, 64, 14));   // amber — warning
    }

    private async void Backup_Click(object sender, RoutedEventArgs e)
    {
        var password = BackupPasswordBox.Password;

        BackupBtn.IsEnabled = false;
        ShowStatus(BackupStatusText, "Creating backup…", Colors.Gray);

        string filePath = "";
        string? error   = null;

        await System.Threading.Tasks.Task.Run(() =>
        {
            try { filePath = BackupService.CreateBackup(_backupFolder, password); }
            catch (Exception ex) { error = ex.Message; }
        });

        BackupBtn.IsEnabled = true;

        if (error is not null)
        {
            Log.Warning("Backup failed: {Error}", error);
            ShowStatus(BackupStatusText, $"Backup failed: {error}", Color.FromRgb(197, 48, 48));
        }
        else
        {
            var fileName  = Path.GetFileName(filePath);
            var encrypted = fileName.EndsWith(".rxb", StringComparison.OrdinalIgnoreCase);
            AuditService.Log("BackupCreated", details: fileName);
            Log.Information("Backup created: {File} (encrypted={Encrypted})", fileName, encrypted);
            var msg = encrypted
                ? $"Encrypted backup saved: {fileName}"
                : $"Backup saved: {fileName}";
            ShowStatus(BackupStatusText, msg, Color.FromRgb(39, 103, 73));
            RefreshBackupList();
        }
    }

    // ── Restore ───────────────────────────────────────────────────────────────

    private void BrowseRestore_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title  = "Select backup file",
            Filter = "Rx Writer Backup|*.zip;*.rxb|Encrypted backup (*.rxb)|*.rxb|Unencrypted backup (*.zip)|*.zip",
            CheckFileExists = true
        };
        if (dlg.ShowDialog() != true) return;

        RestoreFileBox.Text = dlg.FileName;
        RestoreBtn.IsEnabled = true;
        RestoreStatusText.Visibility = Visibility.Collapsed;

        // Show password field only for encrypted .rxb files
        var isEncrypted = dlg.FileName.EndsWith(".rxb", StringComparison.OrdinalIgnoreCase);
        RestorePasswordPanel.Visibility = isEncrypted ? Visibility.Visible : Visibility.Collapsed;
        RestorePasswordBox.Password = "";
    }

    private void Restore_Click(object sender, RoutedEventArgs e)
        => ConfirmAndRestore(RestoreFileBox.Text);

    private void RestoreFromList_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not BackupFile bf) return;

        // For encrypted backups from the list, show password input dialog inline
        if (bf.IsEncrypted)
        {
            RestoreFileBox.Text = bf.Path;
            RestoreBtn.IsEnabled = true;
            RestorePasswordPanel.Visibility = Visibility.Visible;
            RestorePasswordBox.Password = "";
            RestorePasswordBox.Focus();
            ShowStatus(RestoreStatusText,
                "Enter the backup password below, then click 'Restore Database'.",
                Color.FromRgb(21, 101, 192));
            return;
        }

        ConfirmAndRestore(bf.Path);
    }

    private void ConfirmAndRestore(string backupPath)
    {
        if (string.IsNullOrEmpty(backupPath)) return;

        var isEncrypted = backupPath.EndsWith(".rxb", StringComparison.OrdinalIgnoreCase);
        var password    = isEncrypted ? RestorePasswordBox.Password : null;

        if (isEncrypted && string.IsNullOrWhiteSpace(password))
        {
            ShowStatus(RestoreStatusText, "Please enter the backup password to restore an encrypted backup.",
                Color.FromRgb(197, 48, 48));
            RestorePasswordBox.Focus();
            return;
        }

        var result = MessageBox.Show(
            $"This will replace ALL current clinic data with the backup:\n\n{Path.GetFileName(backupPath)}\n\n" +
            "The application will restart automatically.\nAre you sure?",
            "Confirm Restore",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            BackupService.RestoreBackup(backupPath, password);
            AuditService.Log("DatabaseRestored", details: Path.GetFileName(backupPath));
            Log.Information("Database restored from: {File}", Path.GetFileName(backupPath));
            BackupService.RestartApp();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Restore failed for {File}", Path.GetFileName(backupPath));
            ShowStatus(RestoreStatusText, $"Restore failed: {ex.Message}",
                Color.FromRgb(197, 48, 48));
        }
    }

    // ── Backup list ───────────────────────────────────────────────────────────

    private void RefreshList_Click(object sender, RoutedEventArgs e) => RefreshBackupList();

    private void DeleteBackup_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not BackupFile bf) return;

        var result = MessageBox.Show(
            $"Delete backup file?\n{bf.FileName}",
            "Delete Backup", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        try { File.Delete(bf.Path); }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not delete file: {ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        RefreshBackupList();
    }

    private void RefreshBackupList()
    {
        var backups = BackupService.ListBackups(_backupFolder);
        if (backups.Count == 0)
        {
            EmptyText.Visibility  = Visibility.Visible;
            BackupList.Visibility = Visibility.Collapsed;
        }
        else
        {
            EmptyText.Visibility  = Visibility.Collapsed;
            BackupList.Visibility = Visibility.Visible;
            BackupList.ItemsSource = backups;
        }
    }

    private static void ShowStatus(TextBlock tb, string msg, Color color)
    {
        tb.Text = msg;
        tb.Foreground = new SolidColorBrush(color);
        tb.Visibility = Visibility.Visible;
    }

    // ── Merge ─────────────────────────────────────────────────────────────────

    private void MergeBrowse_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title  = "Select backup file to merge",
            Filter = "Rx Writer Backup|*.zip;*.rxb|Encrypted backup (*.rxb)|*.rxb|Unencrypted backup (*.zip)|*.zip",
            CheckFileExists = true
        };
        if (dlg.ShowDialog() != true) return;

        MergeFileBox.Text    = dlg.FileName;
        MergeStatusText.Visibility = Visibility.Collapsed;

        var isEncrypted = dlg.FileName.EndsWith(".rxb", StringComparison.OrdinalIgnoreCase);
        MergePasswordPanel.Visibility = isEncrypted ? Visibility.Visible : Visibility.Collapsed;
        MergePasswordBox.Password = "";

        UpdateMergeButtonState();
    }

    private void UpdateMergeButtonState()
    {
        MergeBtn.IsEnabled =
            !string.IsNullOrWhiteSpace(MergeFileBox.Text) &&
            !string.IsNullOrWhiteSpace(MergeClinicNameBox.Text);
    }

    // Also re-evaluate the button when the clinic name is typed
    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        MergeClinicNameBox.TextChanged += (_, _) => UpdateMergeButtonState();
    }

    private async void Merge_Click(object sender, RoutedEventArgs e)
    {
        var filePath   = MergeFileBox.Text.Trim();
        var clinicName = MergeClinicNameBox.Text.Trim();

        if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(clinicName)) return;

        var isEncrypted = filePath.EndsWith(".rxb", StringComparison.OrdinalIgnoreCase);
        var password    = isEncrypted ? MergePasswordBox.Password : null;

        if (isEncrypted && string.IsNullOrWhiteSpace(password))
        {
            ShowStatus(MergeStatusText,
                "Please enter the backup password for this encrypted file.",
                Color.FromRgb(197, 48, 48));
            MergePasswordBox.Focus();
            return;
        }

        var confirm = MessageBox.Show(
            $"Import all data from:\n{Path.GetFileName(filePath)}\n\n" +
            $"Source clinic tag: \"{clinicName}\"\n\n" +
            "This will ADD records to the master database. No existing data will be changed.",
            "Confirm Merge",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        MergeBtn.IsEnabled = false;
        ShowStatus(MergeStatusText, "Merging — please wait…", Colors.Gray);

        var progress = new Progress<string>(msg =>
            ShowStatus(MergeStatusText, msg, Colors.Gray));

        string? error = null;
        OPDClinic.Services.MergeResult? result = null;

        await System.Threading.Tasks.Task.Run(async () =>
        {
            try
            {
                result = await MergeService.MergeFromBackupAsync(
                    App.DbFactory, filePath, clinicName, password, progress);
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }
        });

        MergeBtn.IsEnabled = true;

        if (error is not null)
        {
            Log.Warning("Merge failed: {Error}", error);
            ShowStatus(MergeStatusText,
                $"Merge failed: {error}",
                Color.FromRgb(197, 48, 48));
        }
        else if (result is not null)
        {
            ShowStatus(MergeStatusText,
                $"✓  {result.Summary}",
                Color.FromRgb(39, 103, 73));
            // Clear the form so the user can load a second clinic without confusion
            MergeFileBox.Text = "";
            MergeClinicNameBox.Text = "";
            MergePasswordBox.Password = "";
            MergePasswordPanel.Visibility = Visibility.Collapsed;
            MergeBtn.IsEnabled = false;
        }
    }
}
