using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace QualityControlSystem.WPF.Models;

public class UserProfileDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Surname { get; set; } = string.Empty;
    public string? Patron { get; set; }
    public string Role { get; set; } = string.Empty;      // admin, operator, equipment specialist, quality control officer
    public int WorkshopId { get; set; }
    public string PersonnelNumber { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
