using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QualityControlSystem.Infrastructure.Entities
{
    [Table("workshop")]
    public class Workshop
    {
        [Key]
        [Column("workshop_id")]
        public int WorkshopId { get; set; }

        [Column("number")]
        public int Number { get; set; }

        [Column("appointment")]
        public string Appointment { get; set; } = null!;
    }
}
