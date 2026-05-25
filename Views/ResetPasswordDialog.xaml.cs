using System.Windows;

namespace OPDClinic.Views;

public partial class ResetPasswordDialog : Window
{
    public string NewPassword { get; private set; } = "";

    public ResetPasswordDialog(string username)
    {
        InitializeComponent();
        SubTitle.Text = $"Set a new password for user '{username}'.\nThe user will be required to change it on next login.";
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Visibility = Visibility.Collapsed;

        var password = PasswordBox.Password;
        var confirm = ConfirmBox.Password;

        if (string.IsNullOrEmpty(password))
        { ShowError("Password is required."); return; }

        if (password.Length < 6)
        { ShowError("Password must be at least 6 characters."); return; }

        if (password != confirm)
        { ShowError("Passwords do not match."); return; }

        NewPassword = password;
        DialogResult = true;
    }

    private void ShowError(string msg)
    {
        ErrorText.Text = msg;
        ErrorText.Visibility = Visibility.Visible;
    }
}
