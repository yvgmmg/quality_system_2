using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QualityControlSystem.Infrastructure.Entities;

[Table("measurement_unit_classifier")]
public partial class MeasurementUnitClassifier
{
    [Key]
    [Column("measurement_unit_id")]
    public int MeasurementUnitId { get; set; }

    [Column("code")]
    [StringLength(3)]
    public string Code { get; set; } = null!;

    [Column("name")]
    [StringLength(255)]
    public string Name { get; set; } = null!;

    [Column("symbol")]
    [StringLength(20)]
    public string Symbol { get; set; } = null!;

    [InverseProperty("MeasurementUnit")]
    public virtual ICollection<Sensor> Sensors { get; set; } = new List<Sensor>();
}
