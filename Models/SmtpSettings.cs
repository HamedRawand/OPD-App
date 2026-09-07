namespace OPDClinic.Models;

/// <summary>Gmail SMTP credentials used for sending password-reset emails.
/// Persisted as JSON to %LocalAppData%\OPDClinic\smtp_settings.json.
/// Defaults below are the shared Rx Writer sender account, baked in so a fresh
/// install works out of the box — an admin can still override via Email Settings
/// (User Management → 📧 Email Settings) to use their own clinic's Gmail account.</summary>
public class SmtpSettings
{
    /// <summary>Gmail address used as the sender (e.g. info.rxwriter@gmail.com).</summary>
    public string SenderEmail { get; set; } = "info.rxwriter@gmail.com";

    /// <summary>Gmail App Password (16-char, no spaces) — NOT the regular Gmail password.</summary>
    public string AppPassword { get; set; } = "ssuzjxgnhfoyrgyj";

    /// <summary>Display name shown in the From field of sent emails.</summary>
    public string SenderName { get; set; } = "Rx Writer Clinic";

    /// <summary>Returns true when enough settings are present to attempt sending.</summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(SenderEmail) &&
        !string.IsNullOrWhiteSpace(AppPassword);
}
