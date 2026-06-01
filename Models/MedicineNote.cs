using System.ComponentModel.DataAnnotations;

namespace OPDClinic.Models;

public class MedicineNote
{
    public int Id { get; set; }
    public string? Notes { get; set; }

    /// <summary>Links to RouteOfAdministration.Category for context-sensitive filtering in the prescription form.</summary>
    [MaxLength(255)]
    public string? Category { get; set; }

    /// <summary>Links to MedicineForm.FormName — filtered by Category in the edit dialog and prescription form.</summary>
    [MaxLength(255)]
    public string? Type { get; set; }
}
