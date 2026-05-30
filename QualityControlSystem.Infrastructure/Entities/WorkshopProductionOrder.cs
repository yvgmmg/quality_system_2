using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QualityControlSystem.Infrastructure.Entities;

[Table("workshop_production_order")]
public partial class WorkshopProductionOrder
{
    [Key]
    [Column("workshop_production_order")]
    public int WorkshopProductionOrder1 { get; set; }

    [Column("workshop_id")]
    public int? WorkshopId { get; set; }

    [Column("production_order_id")]
    public int? ProductionOrderId { get; set; }

    [ForeignKey("ProductionOrderId")]
    [InverseProperty("WorkshopProductionOrders")]
    public virtual ProductionOrder? ProductionOrder { get; set; }

    [ForeignKey("WorkshopId")]
    [InverseProperty("WorkshopProductionOrders")]
    public virtual Workshop? Workshop { get; set; }
}
