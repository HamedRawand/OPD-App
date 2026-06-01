using System.ComponentModel.DataAnnotations;
using OPDClinic.Services;

namespace OPDClinic.Models;

public class CustomRole
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = "";

    [MaxLength(500)]
    public string Description { get; set; } = "";

    /// <summary>
    /// Comma-separated Permission enum names.
    /// e.g. "ViewPatients,PrintPdf,CreateEditPatient"
    /// </summary>
    public string PermissionsJson { get; set; } = "";

    /// <summary>Free-text notes about additional responsibilities beyond the standard list.</summary>
    public string? AdditionalNotes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public HashSet<Permission> GetPermissions()
    {
        if (string.IsNullOrEmpty(PermissionsJson)) return [];
        return PermissionsJson
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => Enum.TryParse<Permission>(s, out var p) ? (Permission?)p : null)
            .Where(p => p.HasValue)
            .Select(p => p!.Value)
            .ToHashSet();
    }

    public void SetPermissions(IEnumerable<Permission> permissions)
    {
        PermissionsJson = string.Join(",", permissions.Select(p => p.ToString()));
    }
}
