using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QualityControlSystem.Infrastructure.Entities
{
    [Table("equipment_inspection_form_production_equipment")]
    public class EquipmentInspectionFormProductionEquipment
    {
        [Key]
        [Column("equipment_inspection_form_production_equipment")]
        public int Id { get; set; }

        [Column("equipment_inspection_form_id")]
        public int? EquipmentInspectionFormId { get; set; }
        public virtual EquipmentInspectionForm? EquipmentInspectionForm { get; set; }

        [Column("production_equipment_id")]
        public int? ProductionEquipmentId { get; set; }
        public virtual ProductionEquipment? ProductionEquipment { get; set; }
    }
}
