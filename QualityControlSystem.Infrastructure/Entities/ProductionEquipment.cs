using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QualityControlSystem.Infrastructure.Entities
{
    [Table("production_equipment")]
    public class ProductionEquipment
    {
        [Key]
        [Column("production_equipment_id")]
        public int ProductionEquipmentId { get; set; }

        [Column("name")]
        public string Name { get; set; } = null!;

        [Column("serial_number")]
        public string? SerialNumber { get; set; }

        [Column("inventory_number")]
        public string? InventoryNumber { get; set; }

        [Column("workshop_id")]
        public int? WorkshopId { get; set; }
        public virtual Workshop? Workshop { get; set; }
    }
}
