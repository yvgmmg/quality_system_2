using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QualityControlSystem.Infrastructure.Entities;

[Table("requisition_invoice")]
public partial class RequisitionInvoice
{
    [Key]
    [Column("requisition_invoice_id")]
    public int RequisitionInvoiceId { get; set; }

    [Column("production_order_id")]
    public int ProductionOrderId { get; set; }

    [Column("creation_date")]
    public DateOnly CreationDate { get; set; }

    [ForeignKey("ProductionOrderId")]
    [InverseProperty("RequisitionInvoices")]
    public virtual ProductionOrder ProductionOrder { get; set; } = null!;

    [InverseProperty("RequisitionInvoice")]
    public virtual ICollection<RequisitionInvoiceItem> RequisitionInvoiceItems { get; set; } = new List<RequisitionInvoiceItem>();
}
