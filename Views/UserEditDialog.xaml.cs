using System.Windows;
using System.Windows.Controls;
using OPDClinic.Helpers;
using OPDClinic.Models;
using OPDClinic.Services;

namespace OPDClinic.Views;

/// <summary>Unified role item that can represent either a built-in UserRole or a CustomRole.</summary>
internal sealed class RoleItem
{
    public string    DisplayName  { get; init; } = "";
    public UserRole  BuiltinRole  { get; init; }
    public int?      CustomRoleId { get; init; }
    public bool      IsCustom     => CustomRoleId.HasValue;
    public override  string ToString() => DisplayName;
}

public partial class UserEditDialog : Window
{
    private readonly User? _existing;
    private readonly bool  _isEdit;
    private List<Physician> _physicians = [];
    private List<RoleItem>  _roleItems  = [];

    public UserEditDialog(User? existing)
    {
        InitializeComponent();
        DialogHelper.ApplyConstraints(this);
        _existing = existing;
        _isEdit   = existing != null;

        using var db = App.DbFactory.CreateDbContext();

        // ── Physicians ───────────────────────────────────────────────────────
        _physicians = db.Physicians
            .OrderBy(p => p.NameEng)
            .Select(p => new Physician { Id = p.Id, NameEng = p.NameEng })
            .ToList();
        PhysicianBox.ItemsSource = _physicians;

        // ── Role items: built-in + custom ────────────────────────────────────
        _roleItems = BuildRoleItems(db);
        RoleBox.ItemsSource       = _roleItems;
        RoleBox.DisplayMemberPath = "DisplayName";
        RoleBox.SelectionChanged += RoleBox_SelectionChanged;

        // ── Pre-fill fields ──────────────────────────────────────────────────
        if (_isEdit)
        {
            TitleText.SetResourceReference(TextBlock.TextProperty, "UserEdit.Header.Edit");
            UsernameBox.Text            = existing!.Username;
            FullNameBox.Text            = existing.FullName;
            EmailBox.Text               = existing.Email ?? "info.rxwriter@gmail.com";
            IsActiveBox.IsChecked       = existing.IsActive;
            MustChangePwdBox.IsChecked  = existing.MustChangePassword;
            PasswordNote.Visibility     = Visibility.Visible;
            PasswordLabel.SetResourceReference(TextBlock.TextProperty, "UserEdit.NewPassword");
            ConfirmLabel.SetResourceReference(TextBlock.TextProperty, "UserEdit.ConfirmNewPassword");

            // Select the correct role item
            RoleItem? toSelect = existing.CustomRoleId.HasValue
                ? _roleItems.FirstOrDefault(r => r.CustomRoleId == existing.CustomRoleId.Value)
                : _roleItems.FirstOrDefault(r => !r.IsCustom && r.BuiltinRole == existing.Role);
            RoleBox.SelectedItem = toSelect ?? _roleItems.FirstOrDefault();

            // Pre-select linked physician if editing a Doctor
            if (existing.PhysicianId.HasValue)
                PhysicianBox.SelectedItem = _physicians.FirstOrDefault(p => p.Id == existing.PhysicianId.Value);
        }
        else
        {
            TitleText.SetResourceReference(TextBlock.TextProperty, "UserEdit.Header.Add");
            PasswordLabel.SetResourceReference(TextBlock.TextProperty, "UserEdit.Password");
            ConfirmLabel.SetResourceReference(TextBlock.TextProperty, "UserEdit.ConfirmPassword");
            RoleBox.SelectedItem = _roleItems.FirstOrDefault(r => !r.IsCustom && r.BuiltinRole == UserRole.Receptionist);
            EmailBox.Text = "info.rxwriter@gmail.com"; // default for new users
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static List<RoleItem> BuildRoleItems(OPDClinic.Data.AppDbContext db)
    {
        var items = new List<RoleItem>();

        // Only full Admins can assign or view the Admin role in the dropdown.
        // CoAdmin cannot create/promote other Admin accounts (privilege escalation prevention).
        if (App.Auth.IsFullAdmin)
            items.Add(new() { DisplayName = "Admin",    BuiltinRole = UserRole.Admin });

        items.Add(new() { DisplayName = "Co-Admin",     BuiltinRole = UserRole.CoAdmin });
        items.Add(new() { DisplayName = "Doctor",       BuiltinRole = UserRole.Doctor });
        items.Add(new() { DisplayName = "Receptionist", BuiltinRole = UserRole.Receptionist });

        // Exclude IsSystem roles (Doctor, Receptionist) — they're already represented above as built-in entries
        var customs = db.CustomRoles.Where(r => !r.IsSystem).OrderBy(r => r.Name).ToList();
        foreach (var cr in customs)
            items.Add(new RoleItem { DisplayName = cr.Name, BuiltinRole = UserRole.Receptionist, CustomRoleId = cr.Id });

        return items;
    }

    // ── Events ───────────────────────────────────────────────────────────────

    private void RoleBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Show physician link only for the built-in Doctor role
        var selected = RoleBox.SelectedItem as RoleItem;
        PhysicianLinkSection.Visibility =
            selected is { IsCustom: false, BuiltinRole: UserRole.Doctor }
                ? Visibility.Visible
                : Visibility.Collapsed;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Visibility = Visibility.Collapsed;

        var username = UsernameBox.Text.Trim();
        var fullName = FullNameBox.Text.Trim();
        var email    = EmailBox.Text.Trim();
        var roleItem = RoleBox.SelectedItem as RoleItem;
        var password = PasswordBox.Password;
        var confirm  = ConfirmBox.Password;

        if (string.IsNullOrEmpty(username))
        { ShowError("Username is required."); return; }

        if (string.IsNullOrEmpty(fullName))
        { ShowError("Full name is required."); return; }

        if (roleItem is null)
        { ShowError("Please select a role."); return; }

        if (!_isEdit && string.IsNullOrEmpty(password))
        { ShowError("Password is required for new users."); return; }

        if (!string.IsNullOrEmpty(password))
        {
            if (password.Length < 8)
            { ShowError("Password must be at least 8 characters."); return; }
            if (password != confirm)
            { ShowError("Passwords do not match."); return; }
        }

        var resolvedRole      = roleItem.BuiltinRole;
        var customRoleId      = roleItem.CustomRoleId;
        var linkedPhysicianId = (roleItem is { IsCustom: false, BuiltinRole: UserRole.Doctor })
            ? (PhysicianBox.SelectedItem as Physician)?.Id
            : null;

        try
        {
            using var db = App.DbFactory.CreateDbContext();

            // Username uniqueness
            var excludeId = _existing?.Id ?? 0;
            if (db.Users.Any(u => u.Username.ToLower() == username.ToLower() && u.Id != excludeId))
            { ShowError($"Username '{username}' is already taken."); return; }

            if (_isEdit)
            {
                var dbUser = db.Users.Find(_existing!.Id)
                    ?? throw new InvalidOperationException("User not found.");

                dbUser.Username           = username;
                dbUser.FullName           = fullName;
                dbUser.Email              = string.IsNullOrWhiteSpace(email) ? null : email;
                dbUser.Role               = resolvedRole;
                dbUser.CustomRoleId       = customRoleId;
                dbUser.PhysicianId        = linkedPhysicianId;
                dbUser.IsActive           = IsActiveBox.IsChecked == true;
                dbUser.MustChangePassword = MustChangePwdBox.IsChecked == true;

                if (!string.IsNullOrEmpty(password))
                {
                    dbUser.PasswordHash        = BCrypt.Net.BCrypt.HashPassword(password);
                    dbUser.IsLocked            = false;
                    dbUser.FailedLoginAttempts = 0;
                }

                // Sync in-memory copy (reflected in parent list)
                _existing!.Username          = username;
                _existing.FullName           = fullName;
                _existing.Email              = string.IsNullOrWhiteSpace(email) ? null : email;
                _existing.Role               = resolvedRole;
                _existing.CustomRoleId       = customRoleId;
                _existing.PhysicianId        = linkedPhysicianId;
                _existing.IsActive           = IsActiveBox.IsChecked == true;
                _existing.MustChangePassword = MustChangePwdBox.IsChecked == true;
            }
            else
            {
                db.Users.Add(new User
                {
                    Username           = username,
                    FullName           = fullName,
                    Email              = string.IsNullOrWhiteSpace(email) ? null : email,
                    Role               = resolvedRole,
                    CustomRoleId       = customRoleId,
                    PhysicianId        = linkedPhysicianId,
                    IsActive           = IsActiveBox.IsChecked == true,
                    MustChangePassword = MustChangePwdBox.IsChecked == true,
                    PasswordHash       = BCrypt.Net.BCrypt.HashPassword(password),
                    CreatedAt          = DateTime.UtcNow
                });
            }

            db.SaveChanges();
            AuditService.Log(_isEdit ? "UserUpdated" : "UserCreated", "User", null, username);
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
        ErrorText.Text       = msg;
        ErrorText.Visibility = Visibility.Visible;
    }
}
