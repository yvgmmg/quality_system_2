using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QualityControlSystem.Infrastructure.Entities
{
    [Table("batch")]
    public class Batch
    {
        [Key]
        [Column("batch_id")]
        public int BatchId { get; set; }

        [Column("number")]
        public int Number { get; set; }

        [Column("production_date")]
        public DateTime ProductionDate { get; set; }

        [Column("production_order_id")]
        public int? ProductionOrderId { get; set; }

        public virtual ProductionOrder? ProductionOrder { get; set; }
    }
}
