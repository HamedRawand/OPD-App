namespace OPDClinic.Models;

public class PatientLabTest
{
    public int Id { get; set; }
    public int VisitId { get; set; }
    public Visit? Visit { get; set; }
    public int LabTestId { get; set; }
    public LabTest? LabTest { get; set; }
}
