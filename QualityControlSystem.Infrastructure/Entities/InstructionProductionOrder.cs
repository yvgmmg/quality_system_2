using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QualityControlSystem.Infrastructure.Entities
{
    [Table("instruction_production_order")]
    public class InstructionProductionOrder
    {
        [Key]
        [Column("instruction_production_order_id")]
        public int Id { get; set; }

        [Column("production_order_id")]
        public int? ProductionOrderId { get; set; }
        public virtual ProductionOrder? ProductionOrder { get; set; }

        [Column("instruction_id")]
        public int? InstructionId { get; set; }
        public virtual Instruction? Instruction { get; set; }
    }
}
