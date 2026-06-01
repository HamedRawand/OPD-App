using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OPDClinic.Models;

/// <summary>
/// Demographic record for a patient — one row per person, persists across visits.
/// Clinical data for each encounter lives in <see cref="Visit"/>.
/// </summary>
public class Patient
{
    public int Id { get; set; }

    /// <summary>Auto-generated unique identifier, e.g. "P-00042". Set after first save.</summary>
    [MaxLength(20)]
    public string? PatientCode { get; set; }

    [MaxLength(255)]
    public string? PatientName { get; set; }

    [MaxLength(50)]
    public string? Sex { get; set; }

    /// <summary>Patient phone number. DB column kept as PatientNumber for backward compatibility.</summary>
    [Column("PatientNumber")]
    [MaxLength(50)]
    public string? PhoneNumber { get; set; }

    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// Set when this patient was imported via a backup merge.
    /// Null for patients entered directly in this database.
    /// </summary>
    [MaxLength(100)]
    public string? SourceClinic { get; set; }

    public ICollection<Visit> Visits { get; set; } = [];
}
