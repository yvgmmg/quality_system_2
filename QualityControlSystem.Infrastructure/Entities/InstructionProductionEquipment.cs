using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QualityControlSystem.Infrastructure.Entities
{
    [Table("instruction_production_equipment")]
    public class InstructionProductionEquipment
    {
        [Key]
        [Column("instruction_production_equipment_id")]
        public int Id { get; set; }

        [Column("production_equipment_id")]
        public int? ProductionEquipmentId { get; set; }
        public virtual ProductionEquipment? ProductionEquipment { get; set; }

        [Column("instruction_id")]
        public int? InstructionId { get; set; }
        public virtual Instruction? Instruction { get; set; }
    }
}
