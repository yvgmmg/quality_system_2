using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QualityControlSystem.Infrastructure.Entities
{
    [Table("workshop_production_order")]
    public class WorkshopProductionOrder
    {
        [Key]
        [Column("workshop_production_order")]
        public int Id { get; set; }

        [Column("workshop_id")]
        public int? WorkshopId { get; set; }
        public virtual Workshop? Workshop { get; set; }

        [Column("production_order_id")]
        public int? ProductionOrderId { get; set; }
        public virtual ProductionOrder? ProductionOrder { get; set; }
    }
}
