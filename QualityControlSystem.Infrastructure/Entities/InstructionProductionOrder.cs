using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QualityControlSystem.Infrastructure.Entities;

[Table("instruction_production_order")]
public partial class InstructionProductionOrder
{
    [Key]
    [Column("instruction_production_order_id")]
    public int InstructionProductionOrderId { get; set; }

    [Column("production_order_id")]
    public int? ProductionOrderId { get; set; }

    [Column("instruction_id")]
    public int? InstructionId { get; set; }

    [ForeignKey("InstructionId")]
    [InverseProperty("InstructionProductionOrders")]
    public virtual Instruction? Instruction { get; set; }

    [ForeignKey("ProductionOrderId")]
    [InverseProperty("InstructionProductionOrders")]
    public virtual ProductionOrder? ProductionOrder { get; set; }
}
