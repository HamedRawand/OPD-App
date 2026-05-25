using System.ComponentModel.DataAnnotations;

namespace OPDClinic.Models;

public enum UserRole { Admin, Doctor, Receptionist }

public class User
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Username { get; set; } = "";

    [Required]
    public string PasswordHash { get; set; } = "";

    [MaxLength(200)]
    public string FullName { get; set; } = "";

    public UserRole Role { get; set; } = UserRole.Receptionist;

    public bool IsActive { get; set; } = true;

    public bool MustChangePassword { get; set; } = false;

    public int FailedLoginAttempts { get; set; } = 0;

    public bool IsLocked { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLogin { get; set; }
}
