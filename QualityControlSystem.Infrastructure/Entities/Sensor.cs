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

    [Column("okof_code")]
    [StringLength(19)]
    public string OkofCode { get; set; } = null!;

    [Column("inventory_number")]
    [StringLength(17)]
    public string InventoryNumber { get; set; } = null!;

    [Column("name")]
    [StringLength(255)]
    public string Name { get; set; } = null!;

    [Column("workshop_id")]
    public int WorkshopId { get; set; }

    [Column("sensor_type_id")]
    public int SensorTypeId { get; set; }

    [Column("measurement_unit_id")]
    public int MeasurementUnitId { get; set; }

    [Column("production_equipment_id")]
    public int? ProductionEquipmentId { get; set; }

    [ForeignKey("MeasurementUnitId")]
    [InverseProperty("Sensors")]
    public virtual MeasurementUnitClassifier MeasurementUnit { get; set; } = null!;

    [ForeignKey("ProductionEquipmentId")]
    [InverseProperty("Sensors")]
    public virtual ProductionEquipment? ProductionEquipment { get; set; }

    [ForeignKey("SensorTypeId")]
    [InverseProperty("Sensors")]
    public virtual SensorTypeClassifier SensorType { get; set; } = null!;

    [ForeignKey("WorkshopId")]
    [InverseProperty("Sensors")]
    public virtual Workshop Workshop { get; set; } = null!;
}
