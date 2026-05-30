using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QualityControlSystem.Infrastructure.Entities;

[Table("production_equipment")]
public partial class ProductionEquipment
{
    [Column("name")]
    [StringLength(255)]
    public string Name { get; set; } = null!;

    [Column("serial_number")]
    [StringLength(100)]
    public string? SerialNumber { get; set; }

    [Column("inventory_number")]
    [StringLength(20)]
    public string? InventoryNumber { get; set; }

    [Column("workshop_id")]
    public int? WorkshopId { get; set; }

    [Key]
    [Column("production_equipment_id")]
    public int ProductionEquipmentId { get; set; }

    [InverseProperty("ProductionEquipment")]
    public virtual ICollection<EquipmentInspectionFormProductionEquipment> EquipmentInspectionFormProductionEquipments { get; set; } = new List<EquipmentInspectionFormProductionEquipment>();

    [InverseProperty("ProductionEquipment")]
    public virtual ICollection<InstructionProductionEquipment> InstructionProductionEquipments { get; set; } = new List<InstructionProductionEquipment>();

    [InverseProperty("ProductionEquipment")]
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    [InverseProperty("ProductionEquipment")]
    public virtual ICollection<RegulatoryInfromationProductionEquipment> RegulatoryInfromationProductionEquipments { get; set; } = new List<RegulatoryInfromationProductionEquipment>();

    [InverseProperty("ProductionEquipment")]
    public virtual ICollection<SensorReading> SensorReadings { get; set; } = new List<SensorReading>();

    [ForeignKey("WorkshopId")]
    [InverseProperty("ProductionEquipments")]
    public virtual Workshop? Workshop { get; set; }
}
