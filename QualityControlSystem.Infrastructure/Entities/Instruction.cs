using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QualityControlSystem.Infrastructure.Entities;

[Table("instruction")]
public partial class Instruction
{
    [Column("name")]
    [StringLength(255)]
    public string Name { get; set; } = null!;

    [Column("content")]
    public string Content { get; set; } = null!;

    [Column("path_to_templates")]
    [StringLength(500)]
    public string PathToTemplates { get; set; } = null!;

    [Key]
    [Column("instruction_id")]
    public int InstructionId { get; set; }

    [InverseProperty("Instruction")]
    public virtual ICollection<Frame> Frames { get; set; } = new List<Frame>();

    [InverseProperty("Instruction")]
    public virtual ICollection<InstructionProductionEquipment> InstructionProductionEquipments { get; set; } = new List<InstructionProductionEquipment>();

    [InverseProperty("Instruction")]
    public virtual ICollection<InstructionProductionOrder> InstructionProductionOrders { get; set; } = new List<InstructionProductionOrder>();

    [InverseProperty("Instruction")]
    public virtual ICollection<RegilatoryInformationInstruction> RegilatoryInformationInstructions { get; set; } = new List<RegilatoryInformationInstruction>();
}
