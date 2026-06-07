using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QualityControlSystem.Infrastructure.Entities;

[Table("material_type")]
public partial class MaterialType
{
    [Key]
    [Column("material_type_id")]
    public int MaterialTypeId { get; set; }

    [Column("code")]
    [StringLength(3)]
    public string Code { get; set; } = null!;

    [Column("name")]
    [StringLength(255)]
    public string Name { get; set; } = null!;

    [InverseProperty("MaterialType")]
    public virtual ICollection<Frame> Frames { get; set; } = new List<Frame>();
}
