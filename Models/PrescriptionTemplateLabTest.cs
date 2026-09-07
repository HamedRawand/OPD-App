namespace OPDClinic.Models;

/// <summary>
/// A lab test included in a <see cref="PrescriptionTemplate"/>. References the LabTest
/// catalog directly (unlike PrescriptionTemplateLine) since the catalog is stable and
/// rarely edited — mirrors the existing <see cref="PatientLabTest"/> join pattern.
/// </summary>
public class PrescriptionTemplateLabTest
{
    public int Id { get; set; }

    public int PrescriptionTemplateId { get; set; }
    public PrescriptionTemplate? Template { get; set; }

    public int LabTestId { get; set; }
    public LabTest? LabTest { get; set; }
}
