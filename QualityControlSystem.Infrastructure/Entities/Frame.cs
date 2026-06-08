using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QualityControlSystem.Infrastructure.Entities;

[Table("frame")]
public partial class Frame
{
    [Key]
    [Column("frame_id")]
    public int FrameId { get; set; }

    [Column("name")]
    [StringLength(255)]
    public string Name { get; set; } = null!;

    [Column("workshop_id")]
    public int WorkshopId { get; set; }

    [Column("material_type_id")]
    public int MaterialTypeId { get; set; }

    [Column("weight", TypeName = "numeric(12, 3)")]
    public decimal? Weight { get; set; }

    [Column("length", TypeName = "numeric(12, 3)")]
    public decimal? Length { get; set; }

    [Column("width", TypeName = "numeric(12, 3)")]
    public decimal? Width { get; set; }

    [Column("height", TypeName = "numeric(12, 3)")]
    public decimal? Height { get; set; }

    [Column("image_path")]
    [StringLength(500)]
    public string? ImagePath { get; set; }

    [InverseProperty("Frame")]
    public virtual ICollection<FrameTestFormFrame> FrameTestFormFrames { get; set; } = new List<FrameTestFormFrame>();

    [InverseProperty("Frame")]
    public virtual ICollection<ProductionEquipmentFrame> ProductionEquipmentFrames { get; set; } = new List<ProductionEquipmentFrame>();

    [ForeignKey("MaterialTypeId")]
    [InverseProperty("Frames")]
    public virtual MaterialType MaterialType { get; set; } = null!;

    [ForeignKey("WorkshopId")]
    [InverseProperty("Frames")]
    public virtual Workshop Workshop { get; set; } = null!;
}
