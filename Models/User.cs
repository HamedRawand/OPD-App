using System.ComponentModel.DataAnnotations;

namespace OPDClinic.Models;

public enum UserRole
{
    Admin        = 0,
    Doctor       = 1,
    Receptionist = 2,
    CoAdmin      = 3,   // Elevated installer/setup role — all Admin permissions minus 4 restricted actions
}

public class User
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Username { get; set; } = "";

    [Required]
    public string PasswordHash { get; set; } = "";

    [MaxLength(200)]
    public string FullName { get; set; } = "";

    [MaxLength(255)]
    public string? Email { get; set; }

    public UserRole Role { get; set; } = UserRole.Receptionist;

    public bool IsActive { get; set; } = true;

    public bool MustChangePassword { get; set; } = false;

    public int FailedLoginAttempts { get; set; } = 0;

    public bool IsLocked { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLogin { get; set; }

    /// <summary>Links a Doctor user to their Physician profile for patient filtering.</summary>
    public int? PhysicianId { get; set; }
    public Physician? Physician { get; set; }

    /// <summary>
    /// When set, this user's permissions come from the CustomRole rather than the built-in Role map.
    /// User.Role is set to Receptionist as the non-admin base for sidebar gating.
    /// </summary>
    public int? CustomRoleId { get; set; }
    public CustomRole? CustomRole { get; set; }

    /// <summary>Custom role name when assigned, otherwise the built-in role enum name.</summary>
    public string RoleDisplayName => CustomRole?.Name ?? Role switch
    {
        UserRole.CoAdmin => "Co-Admin",
        _                => Role.ToString()
    };
}
