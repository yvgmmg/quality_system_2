using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QualityControlSystem.Infrastructure.Entities
{
    [Table("regulatory_infromation_production_equipment")]
    public class RegulatoryInformationProductionEquipment
    {
        [Key]
        [Column("regulatory_information_production_equipment_id")]
        public int Id { get; set; }

        [Column("production_equipment_id")]
        public int? ProductionEquipmentId { get; set; }
        public virtual ProductionEquipment? ProductionEquipment { get; set; }

        [Column("regulatory_information_id")]
        public int? RegulatoryInformationId { get; set; }
        public virtual RegulatoryInformation? RegulatoryInformation { get; set; }
    }
}
