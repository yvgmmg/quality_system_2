using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QualityControlSystem.Infrastructure.Entities
{
    [Table("instruction")]
    public class Instruction
    {
        [Key]
        [Column("instruction_id")]
        public int InstructionId { get; set; }

        [Column("name")]
        public string Name { get; set; } = null!;

        [Column("content")]
        public string Content { get; set; } = null!;

        [Column("path_to_templates")]
        public string PathToTemplates { get; set; } = null!;
    }
}
