using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QualityControlSystem.Infrastructure.Entities
{
    [Table("frame")]
    public class Frame
    {
        [Key]
        [Column("frame_id")]
        public int FrameId { get; set; }

        [Column("production_number")]
        public string ProductionNumber { get; set; } = null!;

        [Column("result")]
        public FrameResult? Result { get; set; }

        [Column("instruction_id")]
        public int? InstructionId { get; set; }
        public virtual Instruction? Instruction { get; set; }

        [Column("batch_id")]
        public int? BatchId { get; set; }
        public virtual Batch? Batch { get; set; }

        [Column("workshop_id")]
        public int? WorkshopId { get; set; }
        public virtual Workshop? Workshop { get; set; }
    }
}
