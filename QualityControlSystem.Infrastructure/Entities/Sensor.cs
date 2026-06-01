using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QualityControlSystem.Infrastructure.Entities;

[Table("sensor")]
public partial class Sensor
{
    [Key]
    [Column("sensor_id")]
    public int SensorId { get; set; }

    [Column("inventory_number")]
    [StringLength(14)]
    public string InventoryNumber { get; set; } = null!;

    [Column("name")]
    [StringLength(255)]
    public string Name { get; set; } = null!;

    [Column("measurement")]
    public Enums.MeasurementUnit Measurement { get; set; }

    [Column("workshop_id")]
    public int WorkshopId { get; set; }

    [Column("sensor_type_id")]
    public Enums.Source SensorTypeId { get; set; }

    [InverseProperty("Sensor")]
    public virtual ICollection<SensorReading> SensorReadings { get; set; } = new List<SensorReading>();

    [ForeignKey("WorkshopId")]
    [InverseProperty("Sensors")]
    public virtual Workshop Workshop { get; set; } = null!;
}
