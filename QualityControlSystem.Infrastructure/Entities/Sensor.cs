using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QualityControlSystem.Infrastructure.Entities
{
    [Table("sensor")]
    public class Sensor
    {
        [Key]
        [Column("sensor_id")]
        public int SensorId { get; set; }

        [Column("inventory_number")]
        public string InventoryNumber { get; set; } = null!;

        [Column("name")]
        public string Name { get; set; } = null!;

        [Column("mesurement")]
        public string Mesurement { get; set; } = null!;

        [Column("workshop_id")]
        public int? WorkshopId { get; set; }
        public virtual Workshop? Workshop { get; set; }

        [Column("sensor_type_id")]
        public string? SensorTypeId { get; set; }
    }
}
