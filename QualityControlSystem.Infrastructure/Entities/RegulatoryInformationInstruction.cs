using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QualityControlSystem.Infrastructure.Entities;

[Table("regulatory_information_instruction")]
public partial class RegulatoryInformationInstruction
{
    [Key]
    [Column("regulatory_information_instruction_id")]
    public int RegulatoryInformationInstructionId { get; set; }

    [Column("regulatory_information_id")]
    public int? RegulatoryInformationId { get; set; }

    [Column("instruction_id")]
    public int? InstructionId { get; set; }

    [ForeignKey("InstructionId")]
    [InverseProperty("RegulatoryInformationInstructions")]
    public virtual Instruction? Instruction { get; set; }

    [ForeignKey("RegulatoryInformationId")]
    [InverseProperty("RegulatoryInformationInstructions")]
    public virtual RegulatoryInformation? RegulatoryInformation { get; set; }
}
