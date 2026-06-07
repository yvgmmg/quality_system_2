using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QualityControlSystem.Infrastructure.Entities;

[Table("user_profile")]
public partial class UserProfile
{
    [Key]
    [Column("user_profile_id")]
    public int UserProfileId { get; set; }

    [Column("workshop_id")]
    public int? WorkshopId { get; set; }

    [Column("role_id")]
    public int RoleId { get; set; }

    [Column("password")]
    [StringLength(255)]
    public string Password { get; set; } = null!;

    [Column("personnel_number")]
    [StringLength(6)]
    public string PersonnelNumber { get; set; } = null!;

    [Column("last_name")]
    [StringLength(100)]
    public string LastName { get; set; } = null!;

    [Column("first_name")]
    [StringLength(100)]
    public string FirstName { get; set; } = null!;

    [Column("middle_name")]
    [StringLength(100)]
    public string? MiddleName { get; set; }

    [ForeignKey("RoleId")]
    [InverseProperty("UserProfiles")]
    public virtual Role Role { get; set; } = null!;

    [ForeignKey("WorkshopId")]
    [InverseProperty("UserProfiles")]
    public virtual Workshop? Workshop { get; set; }
}
