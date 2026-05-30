using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QualityControlSystem.Infrastructure.Entities;

[Table("instruction_production_equipment")]
public partial class InstructionProductionEquipment
{
    [Key]
    [Column("instruction_production_equipment_id")]
    public int InstructionProductionEquipmentId { get; set; }

    [Column("production_equipment_id")]
    public int? ProductionEquipmentId { get; set; }

    [Column("instruction_id")]
    public int? InstructionId { get; set; }

    [ForeignKey("InstructionId")]
    [InverseProperty("InstructionProductionEquipments")]
    public virtual Instruction? Instruction { get; set; }

    [ForeignKey("ProductionEquipmentId")]
    [InverseProperty("InstructionProductionEquipments")]
    public virtual ProductionEquipment? ProductionEquipment { get; set; }
}
