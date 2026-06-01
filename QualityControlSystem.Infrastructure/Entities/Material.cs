using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QualityControlSystem.Infrastructure.Entities;

[Table("material")]
public partial class Material
{
    [Column("account")]
    [StringLength(255)]
    public string? Account { get; set; }

    [Column("name")]
    [StringLength(255)]
    public string Name { get; set; } = null!;

    [Key]
    [Column("material_id")]
    public int MaterialId { get; set; }

    [InverseProperty("Material")]
    public virtual ICollection<RequisitionInvoiceItem> RequisitionInvoiceItems { get; set; } = new List<RequisitionInvoiceItem>();

    [Column("measurement")]
    public Enums.MeasurementUnit Measurement { get; set; }
}
