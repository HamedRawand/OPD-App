using Microsoft.EntityFrameworkCore;
using OPDClinic.Data;
using OPDClinic.Models;

namespace OPDClinic.Services;

public class AuthService(IDbContextFactory<AppDbContext> factory)
{
    private const int MaxFailedAttempts = 5;

    public User? CurrentUser { get; private set; }
    public bool IsLoggedIn => CurrentUser != null;
    public int AttemptsRemaining { get; private set; } = MaxFailedAttempts;

    /// <summary>Non-null when the logged-in user has a CustomRole — used by Can() for O(1) checks.</summary>
    private HashSet<Permission>? _customPermissions;

    public LoginResult Login(string username, string password)
    {
        using var db = factory.CreateDbContext();

        var user = db.Users.FirstOrDefault(u =>
            u.Username.ToLower() == username.ToLower() && u.IsActive);

        if (user is null)
            return LoginResult.InvalidCredentials;

        if (user.IsLocked)
            return LoginResult.AccountLocked;

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= MaxFailedAttempts)
                user.IsLocked = true;
            db.SaveChanges();
            AttemptsRemaining = Math.Max(0, MaxFailedAttempts - user.FailedLoginAttempts);
            return LoginResult.InvalidCredentials;
        }

        user.FailedLoginAttempts = 0;
        user.LastLogin = DateTime.UtcNow;
        db.SaveChanges();

        CurrentUser = user; // detached from context — kept in memory for the session

        // Cache custom role permissions for O(1) Can() checks during the session
        _customPermissions = null;
        if (user.CustomRoleId.HasValue)
        {
            var customRole = db.CustomRoles
                .FirstOrDefault(r => r.Id == user.CustomRoleId.Value);
            _customPermissions = customRole?.GetPermissions();

            // Keep CustomRole reference on CurrentUser for display
            user.CustomRole = customRole;
        }

        if (user.MustChangePassword)
            return LoginResult.MustChangePassword;

        return LoginResult.Success;
    }

    public bool ChangePassword(string currentPassword, string newPassword)
    {
        if (CurrentUser is null) return false;
        if (newPassword.Length < 8) return false;

        if (!BCrypt.Net.BCrypt.Verify(currentPassword, CurrentUser.PasswordHash))
            return false;

        var newHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

        using var db = factory.CreateDbContext();
        var dbUser = db.Users.Find(CurrentUser.Id);
        if (dbUser is null) return false;

        dbUser.PasswordHash      = newHash;
        dbUser.MustChangePassword = false;
        db.SaveChanges();

        // Sync in-memory copy
        CurrentUser.PasswordHash      = newHash;
        CurrentUser.MustChangePassword = false;

        return true;
    }

    public bool VerifyCurrentPassword(string password)
    {
        if (CurrentUser is null) return false;
        return BCrypt.Net.BCrypt.Verify(password, CurrentUser.PasswordHash);
    }

    public void Logout()
    {
        CurrentUser = null;
        _customPermissions = null;
    }

    public bool Can(Permission permission)
    {
        if (CurrentUser is null) return false;
        // Custom role overrides built-in permission map
        if (_customPermissions is not null)
            return _customPermissions.Contains(permission);
        return RolePermissions.Check(CurrentUser.Role, permission);
    }

    /// <summary>Display name for the current user's role — custom role name or enum name.</summary>
    public string CurrentRoleDisplayName =>
        CurrentUser is null ? "" :
        _customPermissions is not null && CurrentUser.CustomRole is not null
            ? CurrentUser.CustomRole.Name
            : CurrentUser.Role.ToString();
}

public enum LoginResult
{
    Success,
    InvalidCredentials,
    AccountLocked,
    MustChangePassword
}

public enum Permission
{
    ViewPatients,
    /// <summary>Register a new patient and add new visits (date, basic info, physician).</summary>
    RegisterPatient,
    /// <summary>Enter and edit clinical data for an existing visit (vitals, findings, diagnosis). Also guards visit deletion.</summary>
    EnterClinicalData,
    WritePrescription,
    PrintPdf,
    ManagePhysicians,
    ManageMedicineCatalog,
    ManageUsers,
    ViewAllPhysicianPatients
}

public static class RolePermissions
{
    private static readonly Dictionary<UserRole, HashSet<Permission>> _map = new()
    {
        [UserRole.Admin] = [
            Permission.ViewPatients,
            Permission.RegisterPatient,
            Permission.EnterClinicalData,
            Permission.WritePrescription,
            Permission.PrintPdf,
            Permission.ManagePhysicians,
            Permission.ManageMedicineCatalog,
            Permission.ManageUsers,
            Permission.ViewAllPhysicianPatients
        ],
        [UserRole.Doctor] = [
            Permission.ViewPatients,
            Permission.RegisterPatient,
            Permission.EnterClinicalData,
            Permission.WritePrescription,
            Permission.PrintPdf
        ],
        [UserRole.Receptionist] = [
            Permission.ViewPatients,
            Permission.RegisterPatient,   // Can register patients but not edit clinical assessments
            Permission.PrintPdf,
            Permission.ViewAllPhysicianPatients
        ]
    };

    public static bool Check(UserRole role, Permission permission) =>
        _map.TryGetValue(role, out var perms) && perms.Contains(permission);
}
