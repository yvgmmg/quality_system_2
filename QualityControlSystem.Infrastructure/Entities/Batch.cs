using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QualityControlSystem.Infrastructure.Entities;

[Table("batch")]
public partial class Batch
{
    [Key]
    [Column("batch_id")]
    public int BatchId { get; set; }

    [Column("number")]
    public int Number { get; set; }

    [Column("production_date")]
    public DateOnly ProductionDate { get; set; }

    [Column("production_order_id")]
    public int? ProductionOrderId { get; set; }

    [InverseProperty("Batch")]
    public virtual ICollection<Frame> Frames { get; set; } = new List<Frame>();

    [ForeignKey("ProductionOrderId")]
    [InverseProperty("Batches")]
    public virtual ProductionOrder? ProductionOrder { get; set; }
}
