using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QualityControlSystem.Infrastructure.Entities
{
    [Table("camera")]
    public class Camera
    {
        [Key]
        [Column("camera_id")]
        public int CameraId { get; set; }

        [Column("inventory_number")]
        public string? InventoryNumber { get; set; }

        [Column("name")]
        public string? Name { get; set; }

        [Column("workshop_id")]
        public int? WorkshopId { get; set; }
        public virtual Workshop? Workshop { get; set; }
    }
}
