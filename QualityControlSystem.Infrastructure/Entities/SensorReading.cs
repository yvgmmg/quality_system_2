using Microsoft.EntityFrameworkCore;
using QualityControlSystem.Infrastructure.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QualityControlSystem.Infrastructure.Entities;

[Table("sensor_readings")]
public partial class SensorReading
{
    [Key]
    [Column("sensor_readings_id")]
    public int SensorReadingsId { get; set; }

    [Column("sensor_id")]
    public int? SensorId { get; set; }

    [Column("value")]
    public double Value { get; set; }

    [Column("readings_time", TypeName = "timestamp without time zone")]
    public DateTime ReadingsTime { get; set; }

    [Column("frame_id")]
    public int? FrameId { get; set; }

    [Column("production_equipment_id")]
    public int? ProductionEquipmentId { get; set; }

    [ForeignKey("FrameId")]
    [InverseProperty("SensorReadings")]
    public virtual Frame? Frame { get; set; }

    [ForeignKey("ProductionEquipmentId")]
    [InverseProperty("SensorReadings")]
    public virtual ProductionEquipment? ProductionEquipment { get; set; }

    [ForeignKey("SensorId")]
    [InverseProperty("SensorReadings")]
    public virtual Sensor? Sensor { get; set; }

    [Column("measurement")]
    public MeasurementUnit Measurement { get; set; }
}
