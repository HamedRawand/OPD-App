using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OPDClinic.Models;

public class Patient
{
    public int Id { get; set; }

    public int? PhysicianId { get; set; }
    public Physician? Physician { get; set; }

    public DateTime? OpdDate { get; set; }

    [MaxLength(50)]
    public string? HijriDate { get; set; }

    [MaxLength(255)]
    public string? PatientName { get; set; }

    public int? Age { get; set; }

    [MaxLength(50)]
    public string? Sex { get; set; }

    [MaxLength(50)]
    public string? PatientNumber { get; set; }

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

    public string? Note { get; set; }

    /// <summary>Text of the selected prescription footer note — stored directly so it prints correctly even if the preset is later deleted.</summary>
    public string? FooterNote { get; set; }

    public DateTime? LastUpdated { get; set; }

    public ICollection<MedicineUsage> Medicines { get; set; } = [];
}
