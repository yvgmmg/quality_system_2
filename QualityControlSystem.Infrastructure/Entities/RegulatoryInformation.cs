using Microsoft.EntityFrameworkCore;
using QualityControlSystem.Infrastructure.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QualityControlSystem.Infrastructure.Entities;

[Table("regulatory_information")]
public partial class RegulatoryInformation
{
    [Key]
    [Column("regulatory_information_id")]
    public int RegulatoryInformationId { get; set; }

    [Column("name")]
    [StringLength(255)]
    public string Name { get; set; } = null!;

    [Column("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Минимально допустимое значение (не менее)
    /// </summary>
    [Column("min_value")]
    public float? MinValue { get; set; }

    /// <summary>
    /// Максимально допустимое значение (не более)
    /// </summary>
    [Column("max_value")]
    public float? MaxValue { get; set; }

    [Column("approval_date")]
    public DateOnly? ApprovalDate { get; set; }

    [Column("end_date")]
    public DateOnly? EndDate { get; set; }

    [InverseProperty("RegulatoryInformation")]
    public virtual ICollection<RegulatoryInformationInstruction> RegulatoryInformationInstructions { get; set; } = new List<RegulatoryInformationInstruction>();

    [InverseProperty("RegulatoryInformation")]
    public virtual ICollection<ParameterResult> ParameterResults { get; set; } = new List<ParameterResult>();

    [InverseProperty("RegulatoryInformation")]
    public virtual ICollection<RegulatoryInfromationProductionEquipment> RegulatoryInfromationProductionEquipments { get; set; } = new List<RegulatoryInfromationProductionEquipment>();

    [Column("type")]
    public Source Type { get; set; }

    [Column("measurement")]
    public MeasurementUnit Measurement { get; set; }
}
