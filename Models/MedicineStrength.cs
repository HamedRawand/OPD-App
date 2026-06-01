using System.ComponentModel.DataAnnotations;

namespace OPDClinic.Models;

public class MedicineStrength
{
    public int Id { get; set; }

    /// <summary>FK → MedicineList. Cascade-deletes when the medicine is deleted.</summary>
    public int MedicineListId { get; set; }

    [MaxLength(100)]
    public string? Value { get; set; }

    public MedicineList? Medicine { get; set; }
}
