using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using OPDClinic.Models;
using OPDClinic.Services;

namespace OPDClinic.ViewModels;

public partial class UserManagementViewModel : ObservableObject
{
    private readonly List<User> _allUsers = [];
    private ICollectionView? _view;

    // Debounce timer — prevents a collection refresh on every keystroke
    private readonly DispatcherTimer _searchDebounce = new()
    {
        Interval = TimeSpan.FromMilliseconds(300)
    };

    public ObservableCollection<User> Users { get; } = [];

    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private string _statusText = "";
    [ObservableProperty] private int    _userCount;

    // ── Authorization properties for XAML bindings ───────────────────────────
    public bool CanAddUser      => App.Auth.Can(Permission.AddUser);
    public bool CanEditUser     => App.Auth.Can(Permission.EditUser);
    public bool CanDeleteUser   => App.Auth.Can(Permission.DeleteUsers);
    /// <summary>Only the full Admin can open Email/SMTP Settings.</summary>
    public bool CanManageSMTP   => App.Auth.IsFullAdmin;

    /// <summary>
    /// Returns true when the current user is CoAdmin and the target user is Admin,
    /// meaning the action must be blocked with a friendly message.
    /// </summary>
    private static bool IsAdminProtected(User user) =>
        user.Role == UserRole.Admin && !App.Auth.IsFullAdmin;

    public UserManagementViewModel()
    {
        _searchDebounce.Tick += (_, _) =>
        {
            _searchDebounce.Stop();
            _view?.Refresh();
        };
    }

    // Debounced — waits 300 ms after the last keystroke before refreshing
    partial void OnSearchTextChanged(string value)
    {
        _searchDebounce.Stop();
        _searchDebounce.Start();
    }

    [RelayCommand]
    public void LoadUsers()
    {
        using var db = App.DbFactory.CreateDbContext();
        _allUsers.Clear();
        _allUsers.AddRange(db.Users.Include(u => u.CustomRole).OrderBy(u => u.Username).ToList());

        Users.Clear();
        foreach (var u in _allUsers) Users.Add(u);

        _view = CollectionViewSource.GetDefaultView(Users);
        _view.Filter = FilterUser;

        UserCount  = _allUsers.Count;
        StatusText = $"{_allUsers.Count} users";
    }

    private bool FilterUser(object obj)
    {
        if (obj is not User u) return false;
        if (string.IsNullOrWhiteSpace(SearchText)) return true;
        var q = SearchText.Trim().ToLower();
        return u.Username.ToLower().Contains(q) ||
               u.FullName.ToLower().Contains(q) ||
               u.RoleDisplayName.ToLower().Contains(q) ||
               (u.Email ?? "").ToLower().Contains(q);
    }

    [RelayCommand]
    public void ToggleActive(User user)
    {
        if (IsAdminProtected(user))
        {
            MessageBox.Show("Administrator accounts can only be managed by the system Admin.",
                "Access Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (user.Id == App.Auth.CurrentUser!.Id)
        {
            MessageBox.Show("You cannot deactivate your own account.",
                "Not Allowed", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var action = user.IsActive ? "deactivate" : "activate";
        var result = MessageBox.Show(
            $"Are you sure you want to {action} user '{user.Username}'?",
            "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        var newValue = !user.IsActive;
        user.IsActive = newValue;
        try
        {
            using var db = App.DbFactory.CreateDbContext();
            var dbUser = db.Users.Find(user.Id);
            if (dbUser != null) { dbUser.IsActive = newValue; db.SaveChanges(); }
        }
        catch (Exception ex)
        {
            user.IsActive = !newValue; // revert optimistic change
            MessageBox.Show($"Could not update user:\n{ex.Message}",
                "Update Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        AuditService.Log(user.IsActive ? "UserActivated" : "UserDeactivated", "User", user.Id, user.Username);
        _view?.Refresh();
    }

    [RelayCommand]
    public void UnlockUser(User user)
    {
        if (IsAdminProtected(user))
        {
            MessageBox.Show("Administrator accounts can only be managed by the system Admin.",
                "Access Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        user.IsLocked = false;
        user.FailedLoginAttempts = 0;
        try
        {
            using var db = App.DbFactory.CreateDbContext();
            var dbUser = db.Users.Find(user.Id);
            if (dbUser != null)
            {
                dbUser.IsLocked = false;
                dbUser.FailedLoginAttempts = 0;
                db.SaveChanges();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not unlock account:\n{ex.Message}",
                "Update Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        AuditService.Log("UserUnlocked", "User", user.Id, user.Username);
        _view?.Refresh();
        MessageBox.Show($"Account '{user.Username}' has been unlocked.",
            "Unlocked", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    [RelayCommand]
    public void ResetPassword(User user)
    {
        if (IsAdminProtected(user))
        {
            MessageBox.Show("Administrator accounts can only be managed by the system Admin.",
                "Access Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var dlg = new Views.ResetPasswordDialog(user.Username);
        if (dlg.ShowDialog() != true) return;

        var newHash = BCrypt.Net.BCrypt.HashPassword(dlg.NewPassword);
        try
        {
            using var db = App.DbFactory.CreateDbContext();
            var dbUser = db.Users.Find(user.Id);
            if (dbUser != null)
            {
                dbUser.PasswordHash        = newHash;
                dbUser.MustChangePassword  = true;
                dbUser.IsLocked            = false;
                dbUser.FailedLoginAttempts = 0;
                db.SaveChanges();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not reset password:\n{ex.Message}",
                "Reset Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        // Sync in-memory copy
        user.PasswordHash        = newHash;
        user.MustChangePassword  = true;
        user.IsLocked            = false;
        user.FailedLoginAttempts = 0;
        AuditService.Log("PasswordReset", "User", user.Id, user.Username);

        MessageBox.Show(
            $"Password reset for '{user.Username}'. They must change it on next login.",
            "Password Reset", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    [RelayCommand]
    public void DeleteUser(User user)
    {
        if (!App.Auth.Can(Permission.DeleteUsers))
        {
            MessageBox.Show("You do not have permission to delete users.",
                "Access Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (IsAdminProtected(user))
        {
            MessageBox.Show("Administrator accounts can only be managed by the system Admin.",
                "Access Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (user.Id == App.Auth.CurrentUser!.Id)
        {
            MessageBox.Show("You cannot delete your own account.",
                "Not Allowed", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var result = MessageBox.Show(
            $"Permanently delete user '{user.Username}'? This cannot be undone.",
            "Delete User", MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        var username = user.Username;
        var userId   = user.Id;
        try
        {
            using var db = App.DbFactory.CreateDbContext();
            db.Remove(user);
            db.SaveChanges();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not delete user:\n{ex.Message}",
                "Delete Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        AuditService.Log("UserDeleted", "User", userId, username);
        Users.Remove(user);
        _allUsers.Remove(user);
        UserCount  = _allUsers.Count;
        StatusText = $"{_allUsers.Count} users";
    }
}
