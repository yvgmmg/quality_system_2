using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QualityControlSystem.Infrastructure.Entities;

[Table("sensor_type_classifier")]
public partial class SensorTypeClassifier
{
    [Key]
    [Column("sensor_type_id")]
    public int SensorTypeId { get; set; }

    [Column("code")]
    [StringLength(2)]
    public string Code { get; set; } = null!;

    [Column("name")]
    [StringLength(255)]
    public string Name { get; set; } = null!;

    [InverseProperty("SensorType")]
    public virtual ICollection<Sensor> Sensors { get; set; } = new List<Sensor>();
}
