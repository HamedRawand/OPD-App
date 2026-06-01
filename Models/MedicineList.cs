using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OPDClinic.Models;

public class MedicineList
{
    public int Id { get; set; }

    [MaxLength(255)]
    public string? MedicineName { get; set; }

    [MaxLength(255)]
    public string? GenericName { get; set; }

    [MaxLength(255)]
    public string? Category { get; set; }

    [MaxLength(255)]
    public string? Type { get; set; }

    /// <summary>Legacy single-strength field — kept for backwards compatibility and migration source.
    /// New code should use <see cref="Strengths"/>.</summary>
    [MaxLength(255)]
    public string? Strength { get; set; }

    public string? Note { get; set; }

    /// <summary>Available strengths for this medicine (one-to-many).</summary>
    public ICollection<MedicineStrength> Strengths { get; set; } = [];

    /// <summary>Display-ready comma-joined strength list; falls back to legacy Strength field if none defined.</summary>
    [NotMapped]
    public string StrengthsDisplay =>
        Strengths.Count > 0
            ? string.Join(", ", Strengths.OrderBy(s => s.Value).Select(s => s.Value ?? ""))
            : Strength ?? "";
}
