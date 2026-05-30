using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QualityControlSystem.Infrastructure.Entities;

[Table("frame_test_form_params")]
public partial class FrameTestFormParam
{
    [Column("frame_test_form_id")]
    public int? FrameTestFormId { get; set; }

    [Column("params_id")]
    public int? ParamsId { get; set; }

    [Key]
    [Column("frame_test_form_params_id")]
    public int FrameTestFormParamsId { get; set; }

    [ForeignKey("FrameTestFormId")]
    [InverseProperty("FrameTestFormParams")]
    public virtual FrameTestForm? FrameTestForm { get; set; }

    [ForeignKey("ParamsId")]
    [InverseProperty("FrameTestFormParams")]
    public virtual Param? Params { get; set; }
}
