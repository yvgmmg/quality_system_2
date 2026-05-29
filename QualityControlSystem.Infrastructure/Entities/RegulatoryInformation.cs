using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QualityControlSystem.Infrastructure.Entities
{
    [Table("regulatory_information")]
    public class RegulatoryInformation
    {
        [Key]
        [Column("regulatory_information_id")]
        public int RegulatoryInformationId { get; set; }

        [Column("name")]
        public string Name { get; set; } = null!;

        [Column("type")]
        public Source Type { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        [Column("min_value")]
        public float? MinValue { get; set; }

        [Column("max_value")]
        public float? MaxValue { get; set; }

        [Column("approval_date")]
        public DateTime? ApprovalDate { get; set; }

        [Column("end_date")]
        public DateTime? EndDate { get; set; }

        [Column("measurement")]
        public MeasurementUnit Measurement { get; set; }
    }
}
