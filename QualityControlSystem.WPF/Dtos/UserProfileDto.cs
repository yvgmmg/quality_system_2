namespace QualityControlSystem.WPF.Dtos;

public class UserProfileDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Surname { get; set; } = string.Empty;
    public string? Patron { get; set; }
    public string Role { get; set; } = string.Empty;      // admin, operator, equipment specialist, quality control
    public string RoleCode { get; set; } = string.Empty;
    public int? WorkshopId { get; set; }
    public string WorkshopName { get; set; } = string.Empty;
    public string PersonnelNumber { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
