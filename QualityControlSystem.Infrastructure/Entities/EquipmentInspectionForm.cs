using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QualityControlSystem.Infrastructure.Entities
{
    [Table("equipment_inspection_form")]
    public class EquipmentInspectionForm
    {
        [Key]
        [Column("equipment_inspection_form_id")]
        public int EquipmentInspectionFormId { get; set; }

        [Column("name")]
        public string Name { get; set; } = null!;

        [Column("creation_date")]
        public DateTime CreationDate { get; set; }

        [Column("complite_date")]
        public DateTime? CompliteDate { get; set; }

        [Column("user_profile_id")]
        public int? UserProfileId { get; set; }
        public virtual UserProfile? UserProfile { get; set; }

        [Column("result")]
        public InspectionResult? Result { get; set; }
    }
}
