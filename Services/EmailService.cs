using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using Serilog;

namespace OPDClinic.Services;

/// <summary>Sends password-reset emails via Gmail SMTP using the configured App Password.</summary>
public static class EmailService
{
    // ── Password generator ─────────────────────────────────────────────────────

    /// <summary>Generates a secure random 10-character temporary password
    /// that contains at least one uppercase, one lowercase, one digit, and one symbol.</summary>
    public static string GenerateTempPassword()
    {
        const string upper   = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower   = "abcdefghjkmnpqrstuvwxyz";
        const string digits  = "23456789";
        const string special = "@#$!";
        const string all     = upper + lower + digits + special;

        var chars = new char[10];
        // Guarantee one of each required class
        chars[0] = Pick(upper,   RandomNumberGenerator.GetInt32(upper.Length));
        chars[1] = Pick(lower,   RandomNumberGenerator.GetInt32(lower.Length));
        chars[2] = Pick(digits,  RandomNumberGenerator.GetInt32(digits.Length));
        chars[3] = Pick(special, RandomNumberGenerator.GetInt32(special.Length));
        for (int i = 4; i < 10; i++)
            chars[i] = Pick(all, RandomNumberGenerator.GetInt32(all.Length));

        // Fisher-Yates shuffle
        for (int i = chars.Length - 1; i > 0; i--)
        {
            int j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }
        return new string(chars);
    }

    private static char Pick(string pool, int index) => pool[index];

    // ── Email sender ──────────────────────────────────────────────────────────

    /// <summary>
    /// Sends a password-reset email containing the temporary password to the user.
    /// Returns null on success, or an error message string on failure.
    /// </summary>
    public static async Task<string?> SendPasswordResetAsync(
        string toEmail, string username, string tempPassword)
    {
        var smtp = SmtpSettingsService.Current;
        if (!smtp.IsConfigured)
            return "Email is not configured. Please ask your administrator to configure Email Settings.";

        try
        {
            using var client = new SmtpClient("smtp.gmail.com", 587)
            {
                EnableSsl   = true,
                Credentials = new NetworkCredential(smtp.SenderEmail, smtp.AppPassword),
                DeliveryMethod    = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false
            };

            var body = $"""
                Dear {username},

                Your Rx Writer password has been reset.

                Your temporary password is:

                    {tempPassword}

                Please sign in with this password. You will be asked to choose a new password immediately after logging in.

                If you did not request this reset, please contact your administrator.

                — Rx Writer Clinic Management System
                """;

            var msg = new MailMessage(
                from: new MailAddress(smtp.SenderEmail, smtp.SenderName),
                to:   new MailAddress(toEmail))
            {
                Subject    = "Rx Writer — Password Reset",
                Body       = body,
                IsBodyHtml = false
            };

            await client.SendMailAsync(msg);
            Log.Information("Password reset email sent to {Email} for user {Username}", toEmail, username);
            return null; // success
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to send password reset email to {Email}", toEmail);
            return $"Failed to send email: {ex.Message}";
        }
    }

    /// <summary>Sends a test email to verify SMTP settings work.</summary>
    public static async Task<string?> SendTestEmailAsync(string toEmail,
        string senderEmail, string appPassword, string senderName)
    {
        try
        {
            using var client = new SmtpClient("smtp.gmail.com", 587)
            {
                EnableSsl             = true,
                Credentials           = new NetworkCredential(senderEmail, appPassword),
                DeliveryMethod        = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false
            };

            var msg = new MailMessage(
                from: new MailAddress(senderEmail, senderName),
                to:   new MailAddress(toEmail))
            {
                Subject    = "Rx Writer — SMTP Test",
                Body       = "This is a test email from Rx Writer. If you received this, your email settings are configured correctly.",
                IsBodyHtml = false
            };

            await client.SendMailAsync(msg);
            return null; // success
        }
        catch (Exception ex)
        {
            return $"Test failed: {ex.Message}";
        }
    }
}
