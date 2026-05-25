using System.Windows;
using System.Windows.Controls;
using OPDClinic.Models;
using OPDClinic.Services;

namespace OPDClinic.Views;

public partial class UserEditDialog : Window
{
    private readonly User? _existing;
    private readonly bool _isEdit;

    public UserEditDialog(User? existing)
    {
        InitializeComponent();
        _existing = existing;
        _isEdit = existing != null;

        RoleBox.ItemsSource = Enum.GetValues<UserRole>();

        if (_isEdit)
        {
            TitleText.SetResourceReference(TextBlock.TextProperty, "UserEdit.Header.Edit");
            UsernameBox.Text = existing!.Username;
            FullNameBox.Text = existing.FullName;
            RoleBox.SelectedItem = existing.Role;
            IsActiveBox.IsChecked = existing.IsActive;
            MustChangePwdBox.IsChecked = existing.MustChangePassword;
            PasswordNote.Visibility = Visibility.Visible;
            PasswordLabel.SetResourceReference(TextBlock.TextProperty, "UserEdit.NewPassword");
            ConfirmLabel.SetResourceReference(TextBlock.TextProperty, "UserEdit.ConfirmNewPassword");
        }
        else
        {
            TitleText.SetResourceReference(TextBlock.TextProperty, "UserEdit.Header.Add");
            PasswordLabel.SetResourceReference(TextBlock.TextProperty, "UserEdit.Password");
            ConfirmLabel.SetResourceReference(TextBlock.TextProperty, "UserEdit.ConfirmPassword");
            RoleBox.SelectedItem = UserRole.Receptionist;
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Visibility = Visibility.Collapsed;

        var username = UsernameBox.Text.Trim();
        var fullName = FullNameBox.Text.Trim();
        var role = (UserRole?)RoleBox.SelectedItem;
        var password = PasswordBox.Password;
        var confirm = ConfirmBox.Password;

        if (string.IsNullOrEmpty(username))
        { ShowError("Username is required."); return; }

        if (string.IsNullOrEmpty(fullName))
        { ShowError("Full name is required."); return; }

        if (role is null)
        { ShowError("Please select a role."); return; }

        // Validate password for new user
        if (!_isEdit && string.IsNullOrEmpty(password))
        { ShowError("Password is required for new users."); return; }

        if (!string.IsNullOrEmpty(password))
        {
            if (password.Length < 8)
            { ShowError("Password must be at least 8 characters."); return; }

            if (password != confirm)
            { ShowError("Passwords do not match."); return; }
        }

        // Check username uniqueness
        var excludeId = _existing?.Id ?? 0;
        var duplicate = App.Db.Users.FirstOrDefault(u =>
            u.Username.ToLower() == username.ToLower() &&
            u.Id != excludeId);
        if (duplicate != null)
        { ShowError($"Username '{username}' is already taken."); return; }

        if (_isEdit)
        {
            _existing!.Username = username;
            _existing.FullName = fullName;
            _existing.Role = role.Value;
            _existing.IsActive = IsActiveBox.IsChecked == true;
            _existing.MustChangePassword = MustChangePwdBox.IsChecked == true;

            if (!string.IsNullOrEmpty(password))
            {
                _existing.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
                _existing.IsLocked = false;
                _existing.FailedLoginAttempts = 0;
            }
        }
        else
        {
            App.Db.Users.Add(new User
            {
                Username = username,
                FullName = fullName,
                Role = role.Value,
                IsActive = IsActiveBox.IsChecked == true,
                MustChangePassword = MustChangePwdBox.IsChecked == true,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                CreatedAt = DateTime.UtcNow
            });
        }

        try
        {
            App.Db.SaveChanges();
            AuditService.Log(
                _isEdit ? "UserUpdated" : "UserCreated",
                "User", null, username);
        }
        catch (Exception ex)
        {
            ShowError($"Could not save user:\n{ex.Message}");
            return;
        }
        DialogResult = true;
    }

    private void ShowError(string msg)
    {
        ErrorText.Text = msg;
        ErrorText.Visibility = Visibility.Visible;
    }
}
