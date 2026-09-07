using System.ComponentModel.DataAnnotations;

namespace OPDClinic.Models;

/// <summary>
/// A single medicine line inside a <see cref="PrescriptionTemplate"/>.
/// Mirrors <see cref="MedicineUsage"/> but denormalized — plain text snapshots,
/// not FKs to MedicineList/Dosage/MedicineNote — so editing or deleting a catalog
/// entry later never silently breaks an existing template.
/// </summary>
public class PrescriptionTemplateLine
{
    public int Id { get; set; }

    public int PrescriptionTemplateId { get; set; }
    public PrescriptionTemplate? Template { get; set; }

    public int SortOrder { get; set; }

    [MaxLength(255)]
    public string? Type { get; set; } // Medicine form (e.g. "Tablet")

    [MaxLength(255)]
    public string? Prescription { get; set; } // Medicine name

    [MaxLength(255)]
    public string? Strength { get; set; }

    public int? Qty { get; set; }

    public string? Usage { get; set; } // Dosage text

    public string? Note { get; set; } // Medicine note
}
