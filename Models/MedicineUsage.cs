using System.ComponentModel.DataAnnotations;

namespace OPDClinic.Models;

public class MedicineUsage
{
    public int Id { get; set; }

    public int VisitId { get; set; }
    public Visit? Visit { get; set; }

    public int LineNumber { get; set; }

    [MaxLength(255)]
    public string? Type { get; set; }

    [MaxLength(255)]
    public string? Prescription { get; set; }

    [MaxLength(255)]
    public string? Strength { get; set; }

    public int? Qty { get; set; }

    public string? Usage { get; set; }

    [MaxLength(255)]
    public string? RouteName { get; set; }

    public string? Note { get; set; }
}
