using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QualityControlSystem.Infrastructure.Entities;

[Table("regulatory_infromation_production_equipment")]
public partial class RegulatoryInfromationProductionEquipment
{
    [Key]
    [Column("regulatory_information_production_equipment_id")]
    public int RegulatoryInformationProductionEquipmentId { get; set; }

    [Column("production_equipment_id")]
    public int? ProductionEquipmentId { get; set; }

    [Column("regulatory_information_id")]
    public int? RegulatoryInformationId { get; set; }

    [ForeignKey("ProductionEquipmentId")]
    [InverseProperty("RegulatoryInfromationProductionEquipments")]
    public virtual ProductionEquipment? ProductionEquipment { get; set; }

    [ForeignKey("RegulatoryInformationId")]
    [InverseProperty("RegulatoryInfromationProductionEquipments")]
    public virtual RegulatoryInformation? RegulatoryInformation { get; set; }
}
