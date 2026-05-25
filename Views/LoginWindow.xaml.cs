using System.Windows;
using OPDClinic.Services;

namespace OPDClinic.Views;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
    }

    private void SignIn_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Visibility = Visibility.Collapsed;

        var username = UsernameBox.Text.Trim();
        var password = PasswordBox.Password;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ShowError("Please enter your username and password.");
            return;
        }

        var result = App.Auth.Login(username, password);

        switch (result)
        {
            case LoginResult.Success:
                OpenMain();
                break;

            case LoginResult.MustChangePassword:
                var dlg = new ChangePasswordDialog();
                dlg.Owner = this;
                if (dlg.ShowDialog() == true)
                    OpenMain();
                break;

            case LoginResult.AccountLocked:
                ShowError("This account is locked after too many failed attempts.\nPlease contact an administrator to unlock it.");
                break;

            case LoginResult.InvalidCredentials:
                var remaining = App.Auth.AttemptsRemaining;
                if (remaining > 0)
                    ShowError($"Invalid username or password. {remaining} attempt{(remaining == 1 ? "" : "s")} remaining before lockout.");
                else
                    ShowError("Invalid username or password.");
                break;
        }
    }

    private void OpenMain()
    {
        var main = new MainWindow();
        main.Show();
        Close();
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }
}
