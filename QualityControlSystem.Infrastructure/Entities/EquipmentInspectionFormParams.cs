using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QualityControlSystem.Infrastructure.Entities
{
    [Table("equipment_inspection_form_params")]
    public class EquipmentInspectionFormParams
    {
        [Key]
        [Column("equipment_inspection_form_params_id")]
        public int Id { get; set; }

        [Column("equipment_inspection_form_id")]
        public int? EquipmentInspectionFormId { get; set; }
        public virtual EquipmentInspectionForm? EquipmentInspectionForm { get; set; }

        [Column("params_id")]
        public int? ParamsId { get; set; }
        public virtual Params? Params { get; set; }
    }
}
