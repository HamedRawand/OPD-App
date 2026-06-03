using System.Windows;
using System.Windows.Media;
using OPDClinic.Helpers;
using OPDClinic.Services;

namespace OPDClinic.Views;

public partial class ForgotPasswordDialog : Window
{
    public ForgotPasswordDialog()
    {
        InitializeComponent();
        DialogHelper.ApplyConstraints(this);
    }

    private async void Reset_Click(object sender, RoutedEventArgs e)
    {
        var username = UsernameBox.Text.Trim();
        var email    = EmailBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(username))
        { ShowStatus("Please enter your username.", isError: true); return; }

        if (string.IsNullOrWhiteSpace(email))
        { ShowStatus("Please enter your registered email address.", isError: true); return; }

        // Validate basic email format
        if (!email.Contains('@') || !email.Contains('.'))
        { ShowStatus("Please enter a valid email address.", isError: true); return; }

        // ── Guard: SMTP must be configured before we touch any password ─────────
        if (!SmtpSettingsService.Current.IsConfigured)
        {
            ShowStatus(
                "Email is not configured.\n\n" +
                "An administrator must set up Email Settings (User Management → 📧 Email Settings) " +
                "before the Forgot Password feature can be used.",
                isError: true);
            return;
        }

        ResetBtn.IsEnabled = false;
        ResetBtn.Content   = "Sending…";
        ShowStatus("Verifying your account…", isError: false);

        // Look up user by username AND email — both must match
        string? tempPwd = null;

        try
        {
            using var db = App.DbFactory.CreateDbContext();
            var user = db.Users.FirstOrDefault(u =>
                u.Username.ToLower() == username.ToLower() &&
                u.Email != null && u.Email.ToLower() == email.ToLower() &&
                u.IsActive);

            if (user is null)
            {
                // Intentionally vague — don't reveal which field didn't match
                ShowStatus("If that username and email match an active account, a reset email has been sent.", isError: false);
                ResetBtn.IsEnabled = true;
                ResetBtn.Content   = "Send Reset Email";
                return;
            }

            // ── Step 1: try to send the email first, before touching the DB ──
            tempPwd = EmailService.GenerateTempPassword();
            username = user.Username; // use exact casing from DB

            var sendError = await EmailService.SendPasswordResetAsync(email, username, tempPwd);
            if (sendError is not null)
            {
                // Email failed — password is NOT yet changed in DB
                ShowStatus($"Could not send reset email:\n{sendError}\n\nYour password has not been changed.", isError: true);
                ResetBtn.IsEnabled = true;
                ResetBtn.Content   = "Send Reset Email";
                return;
            }

            // ── Step 2: email sent successfully — now save the new hash ───────
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

        // ── Success — email sent and password saved ───────────────────────────
        ShowStatus($"✓  A temporary password has been sent to {email}.\nPlease check your inbox and sign in.", isError: false);
        UsernameBox.IsEnabled = false;
        EmailBox.IsEnabled    = false;
        ResetBtn.IsEnabled    = false;

        // Auto-close after 2.5 s so the user returns to the login screen
        var closeTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2.5)
        };
        closeTimer.Tick += (_, _) => { closeTimer.Stop(); Close(); };
        closeTimer.Start();
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
