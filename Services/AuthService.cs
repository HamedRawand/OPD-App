using OPDClinic.Data;
using OPDClinic.Models;

namespace OPDClinic.Services;

public class AuthService(AppDbContext db)
{
    private const int MaxFailedAttempts = 5;

    public User? CurrentUser { get; private set; }
    public bool IsLoggedIn => CurrentUser != null;
    public int AttemptsRemaining { get; private set; } = MaxFailedAttempts;

    public LoginResult Login(string username, string password)
    {
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

        CurrentUser = user;

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

        CurrentUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        CurrentUser.MustChangePassword = false;
        db.SaveChanges();
        return true;
    }

    public bool VerifyCurrentPassword(string password)
    {
        if (CurrentUser is null) return false;
        return BCrypt.Net.BCrypt.Verify(password, CurrentUser.PasswordHash);
    }

    public void Logout() => CurrentUser = null;

    public bool Can(Permission permission) =>
        CurrentUser is not null && RolePermissions.Check(CurrentUser.Role, permission);
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
    CreateEditPatient,
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
            Permission.CreateEditPatient,
            Permission.WritePrescription,
            Permission.PrintPdf,
            Permission.ManagePhysicians,
            Permission.ManageMedicineCatalog,
            Permission.ManageUsers,
            Permission.ViewAllPhysicianPatients
        ],
        [UserRole.Doctor] = [
            Permission.ViewPatients,
            Permission.CreateEditPatient,
            Permission.WritePrescription,
            Permission.PrintPdf
        ],
        [UserRole.Receptionist] = [
            Permission.ViewPatients,
            Permission.CreateEditPatient,
            Permission.PrintPdf,
            Permission.ViewAllPhysicianPatients
        ]
    };

    public static bool Check(UserRole role, Permission permission) =>
        _map.TryGetValue(role, out var perms) && perms.Contains(permission);
}
