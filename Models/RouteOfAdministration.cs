using System.ComponentModel.DataAnnotations;

namespace OPDClinic.Models;

public class RouteOfAdministration
{
    public int Id { get; set; }

    [MaxLength(50)]
    public string? RouteName { get; set; }

    [MaxLength(10)]
    public string? Abbreviation { get; set; }

    [MaxLength(20)]
    public string? Category { get; set; }

    public string? Description { get; set; }
}
