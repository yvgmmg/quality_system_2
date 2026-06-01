using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QualityControlSystem.Infrastructure.Entities;

[Table("parameter_result")]
public partial class ParameterResult
{
    [Key]
    [Column("parameter_result_id")]
    public int ParameterResultId { get; set; }

    [Column("value")]
    public decimal? Value { get; set; }

    [Column("passed")]
    public bool? Passed { get; set; }

    [Column("regulatory_information_id")]
    public int? RegulatoryInformationId { get; set; }

    [InverseProperty("ParameterResult")]
    public virtual ICollection<EquipmentInspectionFormParam> EquipmentInspectionFormParams { get; set; } = new List<EquipmentInspectionFormParam>();

    [InverseProperty("ParameterResult")]
    public virtual ICollection<FrameTestFormParam> FrameTestFormParams { get; set; } = new List<FrameTestFormParam>();

    [ForeignKey("RegulatoryInformationId")]
    [InverseProperty("ParameterResults")]
    public virtual RegulatoryInformation? RegulatoryInformation { get; set; }
}
