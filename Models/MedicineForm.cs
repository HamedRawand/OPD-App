using System.ComponentModel.DataAnnotations;

namespace OPDClinic.Models;

public class MedicineForm
{
    public int Id { get; set; }

    [MaxLength(255)]
    public string? Category { get; set; }

    [MaxLength(255)]
    public string? FormName { get; set; }

    [MaxLength(50)]
    public string? Abbreviation { get; set; }

    [MaxLength(255)]
    public string? Note { get; set; }
}
