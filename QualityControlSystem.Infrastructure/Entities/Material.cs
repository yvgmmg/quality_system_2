using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QualityControlSystem.Infrastructure.Entities;

[Table("material")]
public partial class Material
{
    [Column("requisition-invoice_id")]
    public int? RequisitionInvoiceId { get; set; }

    [Column("account")]
    [StringLength(255)]
    public string Account { get; set; } = null!;

    [Column("name")]
    [StringLength(255)]
    public string Name { get; set; } = null!;

    [Column("count")]
    public int Count { get; set; }

    [Column("price", TypeName = "money")]
    public decimal Price { get; set; }

    [Column("summ", TypeName = "money")]
    public decimal Summ { get; set; }

    [Key]
    [Column("material_id")]
    public int MaterialId { get; set; }

    [ForeignKey("RequisitionInvoiceId")]
    [InverseProperty("Materials")]
    public virtual RequisitionInvoice? RequisitionInvoice { get; set; }
}
