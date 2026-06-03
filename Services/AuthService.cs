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

    /// <summary>
    /// True only for the built-in Admin role.
    /// Use this to gate the four co-admin restrictions:
    ///   • Modify/delete Admin-role users  • SMTP settings  • Import Data  • Merge Clinic Backup
    /// </summary>
    public bool IsFullAdmin => CurrentUser?.Role == UserRole.Admin;

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
            // Explicit custom role assignment takes priority
            var customRole = db.CustomRoles
                .FirstOrDefault(r => r.Id == user.CustomRoleId.Value);
            _customPermissions = customRole?.GetPermissions();
            user.CustomRole = customRole;
        }
        else if (user.Role == UserRole.Doctor || user.Role == UserRole.Receptionist)
        {
            // For built-in Doctor/Receptionist roles, load permissions from the
            // editable system custom role seeded at startup — so admin edits take effect.
            var systemRoleName = user.Role.ToString(); // "Doctor" or "Receptionist"
            var systemRole = db.CustomRoles
                .FirstOrDefault(r => r.IsSystem && r.Name == systemRoleName);
            if (systemRole is not null)
            {
                _customPermissions = systemRole.GetPermissions();
                user.CustomRole    = systemRole;
            }
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
        if (_customPermissions is not null)
        {
            if (_customPermissions.Contains(permission)) return true;
            // Legacy fallback: roles saved before the granular split still grant access via the old combined flags.
            return permission switch
            {
                // Old WritePrescription covers both Add and Edit prescription
                Permission.AddPrescription or Permission.EditPrescription
                    => _customPermissions.Contains(Permission.WritePrescription),

                // EnterClinicalData implied ViewClinicalData in old roles
                Permission.ViewClinicalData
                    => _customPermissions.Contains(Permission.EnterClinicalData),

                // WritePrescription implied ViewPrescription in old roles
                Permission.ViewPrescription
                    => _customPermissions.Contains(Permission.WritePrescription)
                       || _customPermissions.Contains(Permission.AddPrescription)
                       || _customPermissions.Contains(Permission.EditPrescription),

                Permission.ViewPhysicians
                    or Permission.AddPhysician
                    or Permission.EditPhysician
                    => _customPermissions.Contains(Permission.ManagePhysicians),

                Permission.ViewMedicineCatalog
                    or Permission.AddMedicine
                    or Permission.EditMedicine
                    => _customPermissions.Contains(Permission.ManageMedicineCatalog),

                Permission.ViewUsers
                    or Permission.AddUser
                    or Permission.EditUser
                    => _customPermissions.Contains(Permission.ManageUsers),

                _ => false
            };
        }
        return RolePermissions.Check(CurrentUser.Role, permission);
    }

    /// <summary>Returns true if the user has <i>any</i> of the supplied permissions.</summary>
    public bool CanAny(params Permission[] permissions) => permissions.Any(Can);

    /// <summary>Display name for the current user's role — custom role name or enum name.</summary>
    public string CurrentRoleDisplayName =>
        CurrentUser is null ? "" :
        _customPermissions is not null && CurrentUser.CustomRole is not null
            ? CurrentUser.CustomRole.Name
            : CurrentUser.Role == UserRole.CoAdmin ? "Co-Admin"
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
    // ── PATIENTS ──────────────────────────────────────────────────────────────
    ViewPatients,
    RegisterPatient,            // Add new patient + add new visit
    EditPatientInfo,            // Edit patient demographics
    DeletePatient,
    ExportPatients,
    ViewAllPhysicianPatients,

    // ── VISITS / CLINICAL DATA ────────────────────────────────────────────────
    ViewClinicalData,           // View clinical details — vitals, diagnosis (read-only)
    EnterClinicalData,          // Edit vitals, clinical notes, diagnosis
    DeleteVisit,
    ExportVisits,

    // ── PRESCRIPTIONS ─────────────────────────────────────────────────────────
    ViewPrescription,           // View prescription details (read-only)
    AddPrescription,            // Add new medicine lines and lab tests
    EditPrescription,           // Edit existing prescription lines
    DeletePrescriptionLine,
    PrintPdf,

    // ── PHYSICIANS ────────────────────────────────────────────────────────────
    ViewPhysicians,
    AddPhysician,
    EditPhysician,
    DeletePhysicians,
    ExportPhysicians,

    // ── MEDICINE CATALOG ──────────────────────────────────────────────────────
    ViewMedicineCatalog,
    AddMedicine,
    EditMedicine,
    DeleteMedicineCatalog,
    ExportMedicineCatalog,

    // ── USERS ─────────────────────────────────────────────────────────────────
    ViewUsers,
    AddUser,
    EditUser,
    DeleteUsers,
    ExportUsers,

    // ── LEGACY (backward-compat only — hidden in the dialog) ─────────────────
    WritePrescription,          // replaced by AddPrescription + EditPrescription
    ManagePhysicians,
    ManageMedicineCatalog,
    ManageUsers,
}

public static class RolePermissions
{
    private static readonly Dictionary<UserRole, HashSet<Permission>> _map = new()
    {
        [UserRole.Admin] = [
            // Patients
            Permission.ViewPatients,
            Permission.RegisterPatient,
            Permission.EditPatientInfo,
            Permission.DeletePatient,
            Permission.ExportPatients,
            Permission.ViewAllPhysicianPatients,
            // Visits
            Permission.ViewClinicalData,
            Permission.EnterClinicalData,
            Permission.DeleteVisit,
            Permission.ExportVisits,
            // Prescriptions
            Permission.ViewPrescription,
            Permission.AddPrescription,
            Permission.EditPrescription,
            Permission.DeletePrescriptionLine,
            Permission.PrintPdf,
            // Physicians
            Permission.ViewPhysicians,
            Permission.AddPhysician,
            Permission.EditPhysician,
            Permission.DeletePhysicians,
            Permission.ExportPhysicians,
            // Medicine catalog
            Permission.ViewMedicineCatalog,
            Permission.AddMedicine,
            Permission.EditMedicine,
            Permission.DeleteMedicineCatalog,
            Permission.ExportMedicineCatalog,
            // Users
            Permission.ViewUsers,
            Permission.AddUser,
            Permission.EditUser,
            Permission.DeleteUsers,
            Permission.ExportUsers,
        ],
        // Co-Admin: identical permissions to Admin; the 4 UI-level restrictions are enforced
        // via App.Auth.IsFullAdmin checks in ViewModels / code-behind, not via permissions.
        [UserRole.CoAdmin] = [
            Permission.ViewPatients, Permission.RegisterPatient, Permission.EditPatientInfo,
            Permission.DeletePatient, Permission.ExportPatients, Permission.ViewAllPhysicianPatients,
            Permission.ViewClinicalData, Permission.EnterClinicalData, Permission.DeleteVisit, Permission.ExportVisits,
            Permission.ViewPrescription, Permission.AddPrescription, Permission.EditPrescription,
            Permission.DeletePrescriptionLine, Permission.PrintPdf,
            Permission.ViewPhysicians, Permission.AddPhysician, Permission.EditPhysician,
            Permission.DeletePhysicians, Permission.ExportPhysicians,
            Permission.ViewMedicineCatalog, Permission.AddMedicine, Permission.EditMedicine,
            Permission.DeleteMedicineCatalog, Permission.ExportMedicineCatalog,
            Permission.ViewUsers, Permission.AddUser, Permission.EditUser,
            Permission.DeleteUsers, Permission.ExportUsers,
        ],
        // Doctor & Receptionist are now served by seeded system custom roles in DB.
        // These static fallback maps are used only if the system role row is not found.
        [UserRole.Doctor] = [
            Permission.ViewPatients,
            Permission.RegisterPatient,
            Permission.EditPatientInfo,
            Permission.ExportPatients,
            Permission.ViewClinicalData,
            Permission.EnterClinicalData,
            Permission.ExportVisits,
            Permission.ViewPrescription,
            Permission.AddPrescription,
            Permission.EditPrescription,
            Permission.DeletePrescriptionLine,
            Permission.PrintPdf,
            Permission.ViewPhysicians,
            Permission.ViewMedicineCatalog,
        ],
        [UserRole.Receptionist] = [
            Permission.ViewPatients,
            Permission.RegisterPatient,
            Permission.EditPatientInfo,
            Permission.PrintPdf,
            Permission.ViewPhysicians,
            Permission.ViewAllPhysicianPatients,
        ],
    };

    public static bool Check(UserRole role, Permission permission) =>
        _map.TryGetValue(role, out var perms) && perms.Contains(permission);
}
