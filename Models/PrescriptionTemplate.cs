using System.ComponentModel.DataAnnotations;

namespace OPDClinic.Models;

/// <summary>
/// A reusable, admin/doctor-defined prescription preset for a common clinical case
/// (e.g. "UTI — Standard"). Applied to a new, empty visit to pre-fill the Rx lines,
/// lab tests, footer note, and (if still blank) the diagnosis/clinical findings text.
/// </summary>
public class PrescriptionTemplate
{
    public int Id { get; set; }

    [MaxLength(150)]
    public string Name { get; set; } = "";

    [MaxLength(255)]
    public string? Description { get; set; }

    public string? DefaultDiagnosis { get; set; }

    public string? DefaultClinicalFindings { get; set; }

    /// <summary>Text of the selected prescription footer note — stored directly
    /// (same convention as <see cref="Visit.FooterNote"/>) so it survives even if
    /// the catalog entry is later edited or deleted.</summary>
    public string? FooterNote { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PrescriptionTemplateLine>    Lines    { get; set; } = [];
    public ICollection<PrescriptionTemplateLabTest> LabTests { get; set; } = [];
}
