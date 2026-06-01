using System.ComponentModel.DataAnnotations;

namespace OPDClinic.Models;

/// <summary>
/// A single clinical encounter for a patient.
/// All visit-specific data (vitals, diagnosis, medicines, lab tests) lives here.
/// Demographics (name, sex, phone) remain on <see cref="Patient"/>.
/// </summary>
public class Visit
{
    public int Id { get; set; }

    public int PatientId { get; set; }
    public Patient? Patient { get; set; }

    public int? PhysicianId { get; set; }
    public Physician? Physician { get; set; }

    public DateTime? OpdDate { get; set; }

    [MaxLength(50)]
    public string? HijriDate { get; set; }

    public int? Age { get; set; }

    [MaxLength(50)]
    public string? BP { get; set; }

    [MaxLength(50)]
    public string? HR { get; set; }

    [MaxLength(50)]
    public string? PR { get; set; }

    [MaxLength(50)]
    public string? RR { get; set; }

    [MaxLength(50)]
    public string? BT { get; set; }

    [MaxLength(50)]
    public string? BW { get; set; }

    public string? ClinicalFindings { get; set; }

    public string? Diagnosis { get; set; }

    /// <summary>Text of the selected prescription footer note — stored directly
    /// so it prints correctly even if the preset is later deleted.</summary>
    public string? FooterNote { get; set; }

    public string? Note { get; set; }

    public DateTime? LastUpdated { get; set; }

    public ICollection<MedicineUsage> Medicines { get; set; } = [];
    public ICollection<PatientLabTest> LabTests  { get; set; } = [];
}
