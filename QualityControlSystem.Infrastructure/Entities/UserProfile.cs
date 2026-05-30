using Microsoft.EntityFrameworkCore;
using QualityControlSystem.Infrastructure.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QualityControlSystem.Infrastructure.Entities;

[Table("user_profile")]
public partial class UserProfile
{
    [Column("name")]
    [StringLength(255)]
    public string Name { get; set; } = null!;

    [Column("surname")]
    [StringLength(255)]
    public string Surname { get; set; } = null!;

    [Column("patron")]
    [StringLength(255)]
    public string? Patron { get; set; }

    [Column("workshop_id")]
    public int WorkshopId { get; set; }

    [Column("password_hash")]
    [StringLength(255)]
    public string PasswordHash { get; set; } = null!;

    [Column("personnel_number", TypeName = "character varying")]
    public string? PersonnelNumber { get; set; }

    [Key]
    [Column("user_profile_id")]
    public int UserProfileId { get; set; }

    [InverseProperty("UserProfile")]
    public virtual ICollection<EquipmentInspectionForm> EquipmentInspectionForms { get; set; } = new List<EquipmentInspectionForm>();

    [InverseProperty("UserProfile")]
    public virtual ICollection<FrameTestForm> FrameTestForms { get; set; } = new List<FrameTestForm>();

    [InverseProperty("UserProfile")]
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    [InverseProperty("UserProfile")]
    public virtual ICollection<UserProfileAccessRight> UserProfileAccessRights { get; set; } = new List<UserProfileAccessRight>();

    [ForeignKey("WorkshopId")]
    [InverseProperty("UserProfiles")]
    public virtual Workshop Workshop { get; set; } = null!;

    [Column("role")]
    public UserRole Role { get; set; }
}
