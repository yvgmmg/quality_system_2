using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QualityControlSystem.Infrastructure.Entities
{
    [Table("production_order")]
    public class ProductionOrder
    {
        [Key]
        [Column("production_order_id")]
        public int ProductionOrderId { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        [Column("start_date")]
        public DateTime StartDate { get; set; }

        [Column("end_date")]
        public DateTime? EndDate { get; set; }
    }
}
