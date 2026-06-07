using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QualityControlSystem.Infrastructure.Entities;

[Table("frame_test_form_frame")]
public partial class FrameTestFormFrame
{
    [Key]
    [Column("frame_test_form_frame_id")]
    public int FrameTestFormFrameId { get; set; }

    [Column("frame_test_form_id")]
    public int FrameTestFormId { get; set; }

    [Column("frame_id")]
    public int FrameId { get; set; }

    [ForeignKey("FrameId")]
    [InverseProperty("FrameTestFormFrames")]
    public virtual Frame Frame { get; set; } = null!;

    [ForeignKey("FrameTestFormId")]
    [InverseProperty("FrameTestFormFrames")]
    public virtual FrameTestForm FrameTestForm { get; set; } = null!;
}
