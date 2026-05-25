using System.ComponentModel.DataAnnotations;

namespace OPDClinic.Models;

public class LabTest
{
    public int Id { get; set; }

    [MaxLength(255)]
    public string? Category { get; set; }

    [MaxLength(255)]
    public string? TestName { get; set; }

    [MaxLength(50)]
    public string? Abbreviation { get; set; }

    [MaxLength(255)]
    public string? Specimen { get; set; }

    [MaxLength(255)]
    public string? Description { get; set; }
}
