using Microsoft.EntityFrameworkCore;
using QualityControlSystem.Infrastructure.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QualityControlSystem.Infrastructure.Entities;

[Table("frame_test_form")]
public partial class FrameTestForm
{
    [Key]
    [Column("frame_test_form_id")]
    public int FrameTestFormId { get; set; }

    [Column("name")]
    [StringLength(255)]
    public string Name { get; set; } = null!;

    [Column("comments")]
    public string? Comments { get; set; }

    [Column("user_profile_id")]
    public int? UserProfileId { get; set; }

    [InverseProperty("FrameTestForm")]
    public virtual ICollection<FrameTestFormFrame> FrameTestFormFrames { get; set; } = new List<FrameTestFormFrame>();

    [InverseProperty("FrameTestForm")]
    public virtual ICollection<FrameTestFormParam> FrameTestFormParams { get; set; } = new List<FrameTestFormParam>();

    [ForeignKey("UserProfileId")]
    [InverseProperty("FrameTestForms")]
    public virtual UserProfile? UserProfile { get; set; }

    [Column("result")]
    public TestResult Result { get; set; }
}
