using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using OPDClinic.Helpers;
using OPDClinic.Services;

namespace OPDClinic.Views;

public partial class AboutDialog : Window
{
    public AboutDialog()
    {
        InitializeComponent();
        DialogHelper.ApplyConstraints(this);

        var v = Assembly.GetExecutingAssembly().GetName().Version;
        VersionText.Text     = v is null ? "" : $"Version {v.Major}.{v.Minor}.{v.Build}";
        ReleaseDateText.Text = ReleaseNotes.Recent.Length > 0
            ? $"Released  {ReleaseNotes.Recent[0].Date}"
            : "";
        CopyrightText.Text   = $"© {DateTime.Now.Year}  Rx Writer";

        BuildReleaseCards();
        _ = LoadStatsAsync();
    }

    // ── Release note cards ────────────────────────────────────────────────────
    private void BuildReleaseCards()
    {
        foreach (var entry in ReleaseNotes.Recent)
        {
            var isFirst = entry == ReleaseNotes.Recent[0];

            var card = new Border
            {
                Background      = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isFirst ? "#F8FAFF" : "#FAFAFA")),
                BorderBrush     = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0")),
                BorderThickness = new Thickness(1),
                CornerRadius    = new CornerRadius(6),
                Padding         = new Thickness(14, 10, 14, 12),
                Margin          = new Thickness(0, 0, 0, 8),
            };

            var inner = new StackPanel();

            // Version badge + date row
            var headerRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };

            var badge = new Border
            {
                Background      = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isFirst ? "#1565C0" : "#94A3B8")),
                CornerRadius    = new CornerRadius(4),
                Padding         = new Thickness(7, 2, 7, 2),
                Margin          = new Thickness(0, 0, 10, 0),
                Child           = new TextBlock
                {
                    Text       = $"v{entry.Version}",
                    FontSize   = 11,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                }
            };
            headerRow.Children.Add(badge);

            headerRow.Children.Add(new TextBlock
            {
                Text              = entry.Date,
                FontSize          = 11,
                Foreground        = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280")),
                VerticalAlignment = VerticalAlignment.Center,
            });

            if (isFirst)
                headerRow.Children.Add(new Border
                {
                    Background   = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DCFCE7")),
                    CornerRadius = new CornerRadius(10),
                    Padding      = new Thickness(8, 2, 8, 2),
                    Margin       = new Thickness(8, 0, 0, 0),
                    Child        = new TextBlock
                    {
                        Text       = "Latest",
                        FontSize   = 10,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#15803D")),
                    }
                });

            inner.Children.Add(headerRow);

            // Bullet items
            foreach (var item in entry.Items)
                inner.Children.Add(new TextBlock
                {
                    Text         = $"•  {item}",
                    FontSize     = 11,
                    Foreground   = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isFirst ? "#1E293B" : "#64748B")),
                    TextWrapping = TextWrapping.Wrap,
                    Margin       = new Thickness(0, 2, 0, 0),
                });

            card.Child = inner;
            ReleaseCardsPanel.Children.Add(card);
        }
    }

    // ── DB stats ──────────────────────────────────────────────────────────────
    private async Task LoadStatsAsync()
    {
        try
        {
            await Task.Run(() =>
            {
                using var db = App.DbFactory.CreateDbContext();
                var patients  = db.Patients.Count();
                var visits    = db.Visits.Count();
                var medicines = db.MedicineLists.Count();

                Dispatcher.Invoke(() =>
                {
                    PatientCountText.Text  = patients.ToString("N0");
                    VisitCountText.Text    = visits.ToString("N0");
                    MedicineCountText.Text = medicines.ToString("N0");
                });
            });
        }
        catch { /* non-critical — leave dashes */ }
    }

    // ── Check for updates ─────────────────────────────────────────────────────
    private async void CheckUpdates_Click(object sender, RoutedEventArgs e)
    {
        CheckUpdatesBtn.IsEnabled = false;
        CheckUpdatesBtn.Content   = "Checking…";
        try
        {
            var info = await UpdateService.CheckForUpdateAsync();
            if (info is not null)
            {
                Close();
                var dlg = new UpdateAvailableDialog(info) { Owner = Owner };
                dlg.ShowDialog();
            }
            else
            {
                CheckUpdatesBtn.Content = "✓  Up to date";
                await Task.Delay(2000);
                CheckUpdatesBtn.Content   = "Check for Updates";
                CheckUpdatesBtn.IsEnabled = true;
            }
        }
        catch
        {
            CheckUpdatesBtn.Content   = "Check failed — retry";
            CheckUpdatesBtn.IsEnabled = true;
        }
    }

    // ── Links ─────────────────────────────────────────────────────────────────
    private void EmailBtn_Click(object sender, RoutedEventArgs e) =>
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName        = "mailto:info.rxwriter@gmail.com",
            UseShellExecute = true
        });

private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
