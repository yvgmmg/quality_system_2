using QualityControlSystem.Infrastructure.Enums;

namespace QualityControlSystem.Infrastructure.Entities;

public class UserProfile
{
    public int UserProfileId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Surname { get; set; } = string.Empty;
    public string? Patron { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public string PersonnelNumber { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public int WorkshopId { get; set; }
    // Навигационное свойство (если нужно)
    // public Workshop? Workshop { get; set; } // Navigation removed (Workshop entity not defined)
}