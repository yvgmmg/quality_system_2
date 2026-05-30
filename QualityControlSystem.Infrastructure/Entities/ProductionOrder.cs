using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QualityControlSystem.Infrastructure.Entities;

[Table("production_order")]
public partial class ProductionOrder
{
    [Key]
    [Column("production_order_id")]
    public int ProductionOrderId { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("start_date")]
    public DateOnly StartDate { get; set; }

    [Column("end_date")]
    public DateOnly? EndDate { get; set; }

    [InverseProperty("ProductionOrder")]
    public virtual ICollection<Batch> Batches { get; set; } = new List<Batch>();

    [InverseProperty("ProductionOrder")]
    public virtual ICollection<InstructionProductionOrder> InstructionProductionOrders { get; set; } = new List<InstructionProductionOrder>();

    [InverseProperty("ProductionOrder")]
    public virtual ICollection<RequisitionInvoice> RequisitionInvoices { get; set; } = new List<RequisitionInvoice>();

    [InverseProperty("ProductionOrder")]
    public virtual ICollection<WorkshopProductionOrder> WorkshopProductionOrders { get; set; } = new List<WorkshopProductionOrder>();
}
