using System.Windows;
using System.Windows.Media;
using OPDClinic.Services;

namespace OPDClinic.Views;

public partial class ForgotPasswordDialog : Window
{
    public ForgotPasswordDialog()
    {
        InitializeComponent();
    }

    private async void Reset_Click(object sender, RoutedEventArgs e)
    {
        var email = EmailBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(email))
        { ShowStatus("Please enter your email address.", isError: true); return; }

        // Validate basic email format
        if (!email.Contains('@') || !email.Contains('.'))
        { ShowStatus("Please enter a valid email address.", isError: true); return; }

        ResetBtn.IsEnabled = false;
        ResetBtn.Content   = "Sending…";
        ShowStatus("Looking up your account…", isError: false);

        // Look up user by email
        string? username  = null;
        string? tempPwd   = null;

        try
        {
            using var db = App.DbFactory.CreateDbContext();
            var user = db.Users.FirstOrDefault(u =>
                u.Email != null && u.Email.ToLower() == email.ToLower() && u.IsActive);

            if (user is null)
            {
                // Intentionally vague — don't reveal whether email exists
                ShowStatus("If that email is registered to an active account, a reset email has been sent.", isError: false);
                ResetBtn.IsEnabled = true;
                ResetBtn.Content   = "Send Reset Email";
                return;
            }

            // Generate temp password
            tempPwd  = EmailService.GenerateTempPassword();
            username = user.Username;

            // Hash and save
            user.PasswordHash        = BCrypt.Net.BCrypt.HashPassword(tempPwd);
            user.MustChangePassword  = true;
            user.IsLocked            = false;
            user.FailedLoginAttempts = 0;
            db.SaveChanges();

            AuditService.Log("PasswordResetRequested", "User", user.Id, username);
        }
        catch (Exception ex)
        {
            ShowStatus($"An error occurred: {ex.Message}", isError: true);
            ResetBtn.IsEnabled = true;
            ResetBtn.Content   = "Send Reset Email";
            return;
        }

        // Send email
        var sendError = await EmailService.SendPasswordResetAsync(email, username!, tempPwd!);

        ResetBtn.IsEnabled = true;
        ResetBtn.Content   = "Send Reset Email";

        if (sendError is not null)
        {
            // Password was already reset in DB — show error but inform user
            ShowStatus($"Password was reset but the email could not be sent:\n{sendError}\n\nPlease contact your administrator.", isError: true);
        }
        else
        {
            ShowStatus($"✓  A temporary password has been sent to {email}.\nPlease check your inbox and sign in.", isError: false);
            EmailBox.IsEnabled = false;
            ResetBtn.IsEnabled = false;
        }
    }

    private void ShowStatus(string msg, bool isError)
    {
        StatusText.Text = msg;
        if (isError)
        {
            StatusBorder.Background = new SolidColorBrush(Color.FromRgb(254, 215, 215)); // red-100
            StatusText.Foreground   = new SolidColorBrush(Color.FromRgb(197, 48, 48));
        }
        else
        {
            StatusBorder.Background = new SolidColorBrush(Color.FromRgb(198, 246, 213)); // green-100
            StatusText.Foreground   = new SolidColorBrush(Color.FromRgb(39, 103, 73));
        }
        StatusBorder.Visibility = Visibility.Visible;
    }
}
