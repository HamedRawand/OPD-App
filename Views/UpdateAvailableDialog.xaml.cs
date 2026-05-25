using System.Reflection;
using System.Windows;
using OPDClinic.Models;
using OPDClinic.Services;

namespace OPDClinic.Views;

public partial class UpdateAvailableDialog : Window
{
    private readonly UpdateInfo _info;
    private bool _isDownloading;

    public UpdateAvailableDialog(UpdateInfo info)
    {
        InitializeComponent();
        _info = info;
        Populate();
    }

    // ── Setup ─────────────────────────────────────────────────────────────────

    private void Populate()
    {
        var current = Assembly.GetExecutingAssembly().GetName().Version;
        CurrentVersionText.Text = current is null ? "—" : $"v{current.Major}.{current.Minor}.{current.Build}";
        NewVersionText.Text     = $"v{_info.Version}";
        SubtitleText.Text       = string.IsNullOrWhiteSpace(_info.ReleaseName)
            ? _info.Version : _info.ReleaseName;

        ReleaseNotesText.Text   = string.IsNullOrWhiteSpace(_info.ReleaseNotes)
            ? "(No release notes provided.)"
            : _info.ReleaseNotes;
    }

    // ── Button handlers ───────────────────────────────────────────────────────

    private async void UpdateNow_Click(object sender, RoutedEventArgs e)
    {
        if (_isDownloading) return;
        _isDownloading = true;

        UpdateBtn.IsEnabled      = false;
        ProgressPanel.Visibility = Visibility.Visible;
        ErrorBorder.Visibility   = Visibility.Collapsed;
        ProgressText.Text        = (string)FindResource("Update.Downloading");
        DownloadProgress.Value   = 0;

        var progress = new Progress<int>(pct =>
        {
            DownloadProgress.Value = pct;
            if (pct >= 100)
                ProgressText.Text = (string)FindResource("Update.Installing");
        });

        try
        {
            await UpdateService.DownloadAndInstallAsync(_info, progress);
            // App shuts down inside DownloadAndInstallAsync — code below won't run.
        }
        catch (Exception ex)
        {
            _isDownloading          = false;
            UpdateBtn.IsEnabled     = true;
            ProgressPanel.Visibility = Visibility.Collapsed;
            ErrorBorder.Visibility  = Visibility.Visible;
            ErrorText.Text          = ex.Message;
        }
    }

    private void Later_Click(object sender, RoutedEventArgs e)
        => Close();
}
