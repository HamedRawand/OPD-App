namespace OPDClinic.Models;

public class AuditLog
{
    public int     Id         { get; set; }
    public int?    UserId     { get; set; }
    public string  Username   { get; set; } = "";
    public string  Action     { get; set; } = "";
    public string? EntityType { get; set; }
    public int?    EntityId   { get; set; }
    public string? Details    { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
