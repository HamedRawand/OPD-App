using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
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
}
