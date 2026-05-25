namespace OPDClinic.Models;

public class PatientLabTest
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public Patient? Patient { get; set; }
    public int LabTestId { get; set; }
    public LabTest? LabTest { get; set; }
}
