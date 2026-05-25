using System.ComponentModel.DataAnnotations;

namespace OPDClinic.Models;

public class Dosage
{
    public int Id { get; set; }

    public string? DosageText { get; set; }

    [MaxLength(255)]
    public string? Type { get; set; }

    [MaxLength(255)]
    public string? Category { get; set; }
}
