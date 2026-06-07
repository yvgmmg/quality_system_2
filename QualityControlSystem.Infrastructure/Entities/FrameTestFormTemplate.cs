using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QualityControlSystem.Infrastructure.Entities;

[Table("frame_test_form_template")]
public partial class FrameTestFormTemplate
{
    [Key]
    [Column("frame_test_form_template_id")]
    public int FrameTestFormTemplateId { get; set; }

    [Column("frame_test_form_id")]
    public int FrameTestFormId { get; set; }

    [Column("template_id")]
    public int TemplateId { get; set; }

    [ForeignKey("FrameTestFormId")]
    [InverseProperty("FrameTestFormTemplates")]
    public virtual FrameTestForm FrameTestForm { get; set; } = null!;

    [ForeignKey("TemplateId")]
    [InverseProperty("FrameTestFormTemplates")]
    public virtual Template Template { get; set; } = null!;
}
