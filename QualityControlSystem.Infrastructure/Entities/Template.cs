using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QualityControlSystem.Infrastructure.Entities;

[Table("template")]
public partial class Template
{
    [Key]
    [Column("template_id")]
    public int TemplateId { get; set; }

    [Column("name")]
    [StringLength(255)]
    public string Name { get; set; } = null!;

    [Column("image_path")]
    [StringLength(500)]
    public string ImagePath { get; set; } = null!;

    [Column("side", TypeName = "template_side")]
    public string Side { get; set; } = null!;

    [InverseProperty("Template")]
    public virtual ICollection<FrameTestFormTemplate> FrameTestFormTemplates { get; set; } = new List<FrameTestFormTemplate>();
}
