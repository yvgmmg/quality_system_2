using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QualityControlSystem.Infrastructure.Entities;

[Table("check_notification")]
public partial class CheckNotification
{
    [Key]
    [Column("check_notification_id")]
    public int CheckNotificationId { get; set; }

    [Column("production_equipment_id")]
    public int ProductionEquipmentId { get; set; }

    [Column("notification_date")]
    public DateOnly NotificationDate { get; set; }

    [Column("checked_at", TypeName = "timestamp without time zone")]
    public DateTime? CheckedAt { get; set; }

    [Column("defect_percentage", TypeName = "numeric(6, 2)")]
    public decimal? DefectPercentage { get; set; }

    [Column("sensor_value", TypeName = "numeric(12, 3)")]
    public decimal? SensorValue { get; set; }

    [ForeignKey("ProductionEquipmentId")]
    [InverseProperty("CheckNotifications")]
    public virtual ProductionEquipment ProductionEquipment { get; set; } = null!;
}
