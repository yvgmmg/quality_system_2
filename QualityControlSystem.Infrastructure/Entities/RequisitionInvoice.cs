using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QualityControlSystem.Infrastructure.Entities;

[Table("requisition-invoice")]
public partial class RequisitionInvoice
{
    [Key]
    [Column("requisition-invoice_id")]
    public int RequisitionInvoiceId { get; set; }

    [Column("production_order_id")]
    public int? ProductionOrderId { get; set; }

    [Column("creation_date")]
    public DateOnly CreationDate { get; set; }

    [InverseProperty("RequisitionInvoice")]
    public virtual ICollection<Material> Materials { get; set; } = new List<Material>();

    [ForeignKey("ProductionOrderId")]
    [InverseProperty("RequisitionInvoices")]
    public virtual ProductionOrder? ProductionOrder { get; set; }
}
