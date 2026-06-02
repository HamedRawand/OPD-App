using System.Windows;
using System.Windows.Media;
using OPDClinic.Models;
using OPDClinic.Services;

namespace OPDClinic.Views;

public partial class SmtpSettingsDialog : Window
{
    public SmtpSettingsDialog()
    {
        InitializeComponent();

        var s = SmtpSettingsService.Current;
        SenderEmailBox.Text  = s.SenderEmail;
        AppPasswordBox.Password = s.AppPassword;
        SenderNameBox.Text   = s.SenderName;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var email = SenderEmailBox.Text.Trim();
        var pwd   = AppPasswordBox.Password;
        var name  = SenderNameBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(email))
        { ShowStatus("Gmail address is required.", isError: true); return; }

        SmtpSettingsService.Save(new SmtpSettings
        {
            SenderEmail = email,
            AppPassword = pwd,
            SenderName  = string.IsNullOrWhiteSpace(name) ? "Rx Writer Clinic" : name
        });

        ShowStatus("✓  Settings saved successfully.", isError: false);
        DialogResult = true;
    }

    private async void Test_Click(object sender, RoutedEventArgs e)
    {
        var to = TestEmailBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(to))
        { ShowTestStatus("Enter a recipient email address for the test.", isError: true); return; }

        TestBtn.IsEnabled = false;
        TestBtn.Content   = "Sending…";
        ShowTestStatus("Sending test email…", isError: false);

        var error = await EmailService.SendTestEmailAsync(
            to,
            SenderEmailBox.Text.Trim(),
            AppPasswordBox.Password,
            SenderNameBox.Text.Trim());

        TestBtn.IsEnabled = true;
        TestBtn.Content   = "📧 Send Test";

        if (error is null)
            ShowTestStatus($"✓  Test email sent to {to}. Check your inbox.", isError: false);
        else
            ShowTestStatus(error, isError: true);
    }

    private void ShowStatus(string msg, bool isError)
    {
        StatusText.Text       = msg;
        StatusText.Foreground = isError
            ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(197, 48, 48))
            : new SolidColorBrush(System.Windows.Media.Color.FromRgb(39, 103, 73));
        StatusText.Visibility = Visibility.Visible;
    }

    private void ShowTestStatus(string msg, bool isError)
    {
        TestStatusText.Text       = msg;
        TestStatusText.Foreground = isError
            ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(197, 48, 48))
            : new SolidColorBrush(System.Windows.Media.Color.FromRgb(39, 103, 73));
        TestStatusText.Visibility = Visibility.Visible;
    }
}
