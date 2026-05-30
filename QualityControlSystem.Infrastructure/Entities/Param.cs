using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QualityControlSystem.Infrastructure.Entities;

[Table("params")]
public partial class Param
{
    [Key]
    [Column("params_id")]
    public int ParamsId { get; set; }

    [Column("value")]
    public decimal? Value { get; set; }

    [Column("passed")]
    public bool? Passed { get; set; }

    [InverseProperty("Params")]
    public virtual ICollection<EquipmentInspectionFormParam> EquipmentInspectionFormParams { get; set; } = new List<EquipmentInspectionFormParam>();

    [InverseProperty("Params")]
    public virtual ICollection<FrameTestFormParam> FrameTestFormParams { get; set; } = new List<FrameTestFormParam>();
}
