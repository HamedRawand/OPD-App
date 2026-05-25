using System.ComponentModel.DataAnnotations;

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

    [MaxLength(255)]
    public string? Strength { get; set; }

    public string? Note { get; set; }
}
