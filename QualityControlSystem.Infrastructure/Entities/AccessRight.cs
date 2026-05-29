using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QualityControlSystem.Infrastructure.Entities
{
    [Table("access_rights")]
    public class AccessRight
    {
        [Key]
        [Column("access_right_id")]
        public int AccessRightId { get; set; }

        [Column("access_right")]
        public string? AccessRightName { get; set; }
    }
}
