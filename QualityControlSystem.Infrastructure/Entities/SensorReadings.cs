using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QualityControlSystem.Infrastructure.Entities
{
    [Table("sensor_readings")]
    public class SensorReadings
    {
        [Key]
        [Column("sensor_readings_id")]
        public int SensorReadingsId { get; set; }

        [Column("sensor_id")]
        public int? SensorId { get; set; }
        public virtual Sensor? Sensor { get; set; }

        [Column("value")]
        public double Value { get; set; }

        [Column("readings_time")]
        public DateTime ReadingsTime { get; set; }

        [Column("frame_id")]
        public int? FrameId { get; set; }
        public virtual Frame? Frame { get; set; }

        [Column("production_equipment_id")]
        public int? ProductionEquipmentId { get; set; }
        public virtual ProductionEquipment? ProductionEquipment { get; set; }

        [Column("measurement")]
        public MeasurementUnit Measurement { get; set; }
    }
}
