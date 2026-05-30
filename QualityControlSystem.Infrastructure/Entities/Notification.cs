using Microsoft.EntityFrameworkCore;
using QualityControlSystem.Infrastructure.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QualityControlSystem.Infrastructure.Entities;

[Table("notification")]
public partial class Notification
{
    [Key]
    [Column("notification_id")]
    public int NotificationId { get; set; }

    [Column("name")]
    [StringLength(255)]
    public string Name { get; set; } = null!;

    [Column("description")]
    [StringLength(255)]
    public string? Description { get; set; }

    [Column("user_profile_id")]
    public int? UserProfileId { get; set; }

    [Column("frame_id")]
    public int? FrameId { get; set; }

    [Column("production_equipment_id")]
    public int? ProductionEquipmentId { get; set; }

    [Column("notification_time", TypeName = "timestamp without time zone")]
    public DateTime NotificationTime { get; set; }

    [ForeignKey("FrameId")]
    [InverseProperty("Notifications")]
    public virtual Frame? Frame { get; set; }

    [ForeignKey("ProductionEquipmentId")]
    [InverseProperty("Notifications")]
    public virtual ProductionEquipment? ProductionEquipment { get; set; }

    [ForeignKey("UserProfileId")]
    [InverseProperty("Notifications")]
    public virtual UserProfile? UserProfile { get; set; }

    [Column("source")]
    public Source Source { get; set; }

    [Column("severity")]
    public Severity Severity { get; set; }
}
