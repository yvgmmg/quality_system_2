using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QualityControlSystem.Infrastructure.Entities
{
    [Table("params")]
    public class Params
    {
        [Key]
        [Column("params_id")]
        public int ParamsId { get; set; }

        [Column("value")]
        public decimal? Value { get; set; }

        [Column("passed")]
        public bool? Passed { get; set; }
    }
}
