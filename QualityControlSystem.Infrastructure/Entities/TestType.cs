using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QualityControlSystem.Infrastructure.Entities;

[Table("test_type")]
public partial class TestType
{
    [Key]
    [Column("test_type_id")]
    public int TestTypeId { get; set; }

    [Column("name")]
    [StringLength(255)]
    public string Name { get; set; } = null!;

    [InverseProperty("TestType")]
    public virtual ICollection<FrameTestForm> FrameTestForms { get; set; } = new List<FrameTestForm>();
}
