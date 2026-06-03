using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OPDClinic.Models;
using OPDClinic.Services;
using OPDClinic.Views;

namespace OPDClinic.ViewModels;

public partial class CustomRolesViewModel : ObservableObject
{
    [ObservableProperty] private ObservableCollection<CustomRole> _roles = [];
    [ObservableProperty] private CustomRole? _selectedRole;

    public CustomRolesViewModel() => LoadRoles();

    [RelayCommand]
    public void LoadRoles()
    {
        using var db = App.DbFactory.CreateDbContext();
        var roles = db.CustomRoles
            .OrderByDescending(r => r.IsSystem)
            .ThenBy(r => r.Name)
            .ToList();
        Roles = new ObservableCollection<CustomRole>(roles);
        SelectedRole = null;
    }

    [RelayCommand]
    private void AddRole()
    {
        var dlg = new CustomRoleEditDialog(null);
        if (dlg.ShowDialog() == true) LoadRoles();
    }

    [RelayCommand]
    private void EditRole(CustomRole? role)
    {
        if (role is null) return;
        var dlg = new CustomRoleEditDialog(role);
        if (dlg.ShowDialog() == true) LoadRoles();
    }

    [RelayCommand]
    private void DeleteRole(CustomRole? role)
    {
        if (role is null) return;

        if (role.IsSystem)
        {
            MessageBox.Show(
                $"The \"{role.Name}\" role is a built-in role and cannot be deleted.\n\n" +
                "You can edit its permissions using the ✏ button.",
                "Cannot Delete Built-in Role",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // Find affected users before showing the warning
        List<string> affectedNames;
        using (var checkDb = App.DbFactory.CreateDbContext())
        {
            affectedNames = checkDb.Users
                .Where(u => u.CustomRoleId == role.Id)
                .Select(u => u.FullName ?? u.Username)
                .ToList();
        }

        var userList = affectedNames.Count switch
        {
            0 => "No users are currently assigned this role.",
            1 => $"1 user will revert to Receptionist:\n  • {affectedNames[0]}",
            _ => $"{affectedNames.Count} users will revert to Receptionist:\n" +
                 string.Join("\n", affectedNames.Take(10).Select(n => $"  • {n}")) +
                 (affectedNames.Count > 10 ? $"\n  … and {affectedNames.Count - 10} more" : "")
        };

        var result = MessageBox.Show(
            $"Delete role \"{role.Name}\"?\n\n{userList}",
            "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        try
        {
            using var db = App.DbFactory.CreateDbContext();
            // Reset any users assigned this role
            var affected = db.Users.Where(u => u.CustomRoleId == role.Id).ToList();
            foreach (var u in affected)
            {
                u.CustomRoleId = null;
                u.Role = UserRole.Receptionist;
            }
            db.Remove(role);
            db.SaveChanges();
            AuditService.Log("CustomRoleDeleted", "CustomRole", role.Id, role.Name);
            LoadRoles();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not delete role:\n{ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
