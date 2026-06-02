using System.ComponentModel.DataAnnotations;

namespace OPDClinic.Models;

public class Physician
{
    public int Id { get; set; }

    [MaxLength(255)]
    public string? NameEng { get; set; }

    [MaxLength(255)]
    public string? NameDari { get; set; }

    [MaxLength(255)]
    public string? SpecialityEng { get; set; }

    [MaxLength(255)]
    public string? SpecialityDari { get; set; }

    [MaxLength(255)]
    public string? OtherSpecialityEng { get; set; }

    [MaxLength(255)]
    public string? OtherSpecialityDari { get; set; }

    [MaxLength(255)]
    public string? ClinicNameEng { get; set; }

    [MaxLength(255)]
    public string? ClinicNameDari { get; set; }

    /// <summary>Short tagline / motto printed below the logo in the PDF header (optional).</summary>
    [MaxLength(500)]
    public string? Tagline { get; set; }

    /// <summary>Raw bytes of the physician's logo/stamp image (PNG, JPG, etc.)
    /// Stored as BLOB in SQLite. Used in the prescription PDF header.</summary>
    public byte[]? SymbolImage { get; set; }

    [MaxLength(255)]
    public string? ContactNumber { get; set; }

    [MaxLength(255)]
    public string? WhatsAppNumber { get; set; }

    [MaxLength(255)]
    public string? ReceptionContactNumber { get; set; }

    [MaxLength(255)]
    public string? Address { get; set; }

    public ICollection<Visit> Visits { get; set; } = [];
}
