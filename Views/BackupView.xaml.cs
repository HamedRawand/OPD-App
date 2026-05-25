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

    private async void Backup_Click(object sender, RoutedEventArgs e)
    {
        BackupBtn.IsEnabled = false;
        ShowStatus(BackupStatusText, "Creating backup…", Colors.Gray);

        string zipPath = "";
        string? error = null;

        await System.Threading.Tasks.Task.Run(() =>
        {
            try { zipPath = BackupService.CreateBackup(_backupFolder); }
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
            var fileName = Path.GetFileName(zipPath);
            AuditService.Log("BackupCreated", details: fileName);
            Log.Information("Backup created: {File}", fileName);
            ShowStatus(BackupStatusText, $"Backup saved: {fileName}", Color.FromRgb(39, 103, 73));
            RefreshBackupList();
        }
    }

    // ── Restore ───────────────────────────────────────────────────────────────

    private void BrowseRestore_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Select backup file",
            Filter = "OPD Clinic Backup (*.zip)|*.zip",
            CheckFileExists = true
        };
        if (dlg.ShowDialog() != true) return;

        RestoreFileBox.Text = dlg.FileName;
        RestoreBtn.IsEnabled = true;
        RestoreStatusText.Visibility = Visibility.Collapsed;
    }

    private void Restore_Click(object sender, RoutedEventArgs e)
    {
        ConfirmAndRestore(RestoreFileBox.Text);
    }

    private void RestoreFromList_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not BackupFile bf) return;
        ConfirmAndRestore(bf.Path);
    }

    private void ConfirmAndRestore(string zipPath)
    {
        if (string.IsNullOrEmpty(zipPath)) return;

        var result = MessageBox.Show(
            $"This will replace ALL current clinic data with the backup:\n\n{Path.GetFileName(zipPath)}\n\n" +
            "The application will restart automatically.\nAre you sure?",
            "Confirm Restore",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            BackupService.RestoreBackup(zipPath);
            AuditService.Log("DatabaseRestored", details: Path.GetFileName(zipPath));
            Log.Information("Database restored from: {File}", Path.GetFileName(zipPath));
            BackupService.RestartApp();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Restore failed for {File}", Path.GetFileName(zipPath));
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
