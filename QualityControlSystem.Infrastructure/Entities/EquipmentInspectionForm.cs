using Microsoft.EntityFrameworkCore;
using QualityControlSystem.Infrastructure.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QualityControlSystem.Infrastructure.Entities;

[Table("equipment_inspection_form")]
public partial class EquipmentInspectionForm
{
    [Key]
    [Column("equipment_inspection_form_id")]
    public int EquipmentInspectionFormId { get; set; }

    [Column("name")]
    [StringLength(255)]
    public string Name { get; set; } = null!;

    [Column("creation_date")]
    public DateOnly CreationDate { get; set; }

    [Column("complite_date")]
    public DateOnly? CompliteDate { get; set; }

    [Column("user_profile_id")]
    public int? UserProfileId { get; set; }

    [InverseProperty("EquipmentInspectionForm")]
    public virtual ICollection<EquipmentInspectionFormParam> EquipmentInspectionFormParams { get; set; } = new List<EquipmentInspectionFormParam>();

    [InverseProperty("EquipmentInspectionForm")]
    public virtual ICollection<EquipmentInspectionFormProductionEquipment> EquipmentInspectionFormProductionEquipments { get; set; } = new List<EquipmentInspectionFormProductionEquipment>();

    [ForeignKey("UserProfileId")]
    [InverseProperty("EquipmentInspectionForms")]
    public virtual UserProfile? UserProfile { get; set; }

    [Column("result")]
    public InspectionResult? Result { get; set; }
}
