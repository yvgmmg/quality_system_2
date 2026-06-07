using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

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

    [Column("description")]
    public string? Description { get; set; }

    [Column("test_type_id")]
    public int TestTypeId { get; set; }

    [InverseProperty("FrameTestForm")]
    public virtual ICollection<FrameTestFormFrame> FrameTestFormFrames { get; set; } = new List<FrameTestFormFrame>();

    [InverseProperty("FrameTestForm")]
    public virtual ICollection<FrameTestFormTemplate> FrameTestFormTemplates { get; set; } = new List<FrameTestFormTemplate>();

    [ForeignKey("TestTypeId")]
    [InverseProperty("FrameTestForms")]
    public virtual TestType TestType { get; set; } = null!;
}
