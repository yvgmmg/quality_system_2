using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QualityControlSystem.Infrastructure.Entities
{
    [Table("notification")]
    public class Notification
    {
        [Key]
        [Column("notification_id")]
        public int NotificationId { get; set; }

        [Column("title")]
        public string? Title { get; set; }

        [Column("text")]
        public string? Text { get; set; }

        [Column("frame_id")]
        public int? FrameId { get; set; }
        public virtual Frame? Frame { get; set; }

        [Column("production_equipment_id")]
        public int? ProductionEquipmentId { get; set; }
        public virtual ProductionEquipment? ProductionEquipment { get; set; }
    }
}
