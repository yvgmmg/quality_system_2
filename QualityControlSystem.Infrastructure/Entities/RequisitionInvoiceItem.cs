using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QualityControlSystem.Infrastructure.Entities;

[Table("requisition_invoice_item")]
public partial class RequisitionInvoiceItem
{
    [Key]
    [Column("requisition_invoice_item_id")]
    public int RequisitionInvoiceItemId { get; set; }

    [Column("requisition_invoice_id")]
    public int RequisitionInvoiceId { get; set; }

    [Column("material_id")]
    public int MaterialId { get; set; }

    [ForeignKey("MaterialId")]
    [InverseProperty("RequisitionInvoiceItems")]
    public virtual Material Material { get; set; } = null!;

    [ForeignKey("RequisitionInvoiceId")]
    [InverseProperty("RequisitionInvoiceItems")]
    public virtual RequisitionInvoice RequisitionInvoice { get; set; } = null!;
}
