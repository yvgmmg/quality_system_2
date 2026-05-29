using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QualityControlSystem.Infrastructure.Entities
{
    [Table("regilatory_information_instruction")]
    public class RegulatoryInformationInstruction
    {
        [Key]
        [Column("regulatory_information_instruction_id")]
        public int Id { get; set; }

        [Column("regulatory_information_id")]
        public int? RegulatoryInformationId { get; set; }
        public virtual RegulatoryInformation? RegulatoryInformation { get; set; }

        [Column("instruction_id")]
        public int? InstructionId { get; set; }
        public virtual Instruction? Instruction { get; set; }
    }
}
