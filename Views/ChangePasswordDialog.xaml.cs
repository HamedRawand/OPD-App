using System.Windows;
using OPDClinic.Services;

namespace OPDClinic.Views;

public partial class ChangePasswordDialog : Window
{
    public ChangePasswordDialog()
    {
        InitializeComponent();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Visibility = Visibility.Collapsed;

        var current = CurrentPwdBox.Password;
        var newPwd = NewPwdBox.Password;
        var confirm = ConfirmPwdBox.Password;

        if (string.IsNullOrEmpty(current))
        { ShowError("Please enter your current password."); return; }

        if (string.IsNullOrEmpty(newPwd))
        { ShowError("Please enter a new password."); return; }

        if (newPwd.Length < 8)
        { ShowError("New password must be at least 8 characters."); return; }

        if (newPwd == current)
        { ShowError("New password must be different from the current password."); return; }

        if (newPwd != confirm)
        { ShowError("Passwords do not match."); return; }

        if (!App.Auth.ChangePassword(current, newPwd))
        { ShowError("Current password is incorrect."); return; }

        AuditService.Log("PasswordChangedSelf", "User", App.Auth.CurrentUser?.Id, App.Auth.CurrentUser?.Username);
        DialogResult = true;
    }

    private void ShowError(string msg)
    {
        ErrorText.Text = msg;
        ErrorText.Visibility = Visibility.Visible;
    }
}
