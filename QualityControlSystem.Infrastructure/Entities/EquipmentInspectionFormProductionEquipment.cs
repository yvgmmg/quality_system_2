using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QualityControlSystem.Infrastructure.Entities;

[Table("equipment_inspection_form_production_equipment")]
public partial class EquipmentInspectionFormProductionEquipment
{
    [Key]
    [Column("equipment_inspection_form_production_equipment")]
    public int EquipmentInspectionFormProductionEquipment1 { get; set; }

    [Column("equipment_inspection_form_id")]
    public int? EquipmentInspectionFormId { get; set; }

    [Column("production_equipment_id")]
    public int? ProductionEquipmentId { get; set; }

    [ForeignKey("EquipmentInspectionFormId")]
    [InverseProperty("EquipmentInspectionFormProductionEquipments")]
    public virtual EquipmentInspectionForm? EquipmentInspectionForm { get; set; }

    [ForeignKey("ProductionEquipmentId")]
    [InverseProperty("EquipmentInspectionFormProductionEquipments")]
    public virtual ProductionEquipment? ProductionEquipment { get; set; }
}
