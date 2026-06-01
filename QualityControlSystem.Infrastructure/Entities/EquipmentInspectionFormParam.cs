using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QualityControlSystem.Infrastructure.Entities;

[Table("equipment_inspection_form_params")]
public partial class EquipmentInspectionFormParam
{
    [Key]
    [Column("equipment_inspection_form_params_id")]
    public int EquipmentInspectionFormParamsId { get; set; }

    [Column("equipment_inspection_form_id")]
    public int? EquipmentInspectionFormId { get; set; }

    [Column("parameter_result_id")]
    public int? ParameterResultId { get; set; }

    [ForeignKey("EquipmentInspectionFormId")]
    [InverseProperty("EquipmentInspectionFormParams")]
    public virtual EquipmentInspectionForm? EquipmentInspectionForm { get; set; }

    [ForeignKey("ParameterResultId")]
    [InverseProperty("EquipmentInspectionFormParams")]
    public virtual ParameterResult? ParameterResult { get; set; }
}
