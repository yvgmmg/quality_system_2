using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QualityControlSystem.Infrastructure.Entities
{
    [Table("material")]
    public class Material
    {
        [Key]
        [Column("material_id")]
        public int MaterialId { get; set; }

        [Column("name")]
        public string? Name { get; set; }

        [Column("price")]
        public decimal? Price { get; set; }

        [Column("count")]
        public int? Count { get; set; }

        // Сумма считается в коде, не хранится в БД
        [NotMapped]
        public decimal? Summ => (Price ?? 0) * (Count ?? 0);
    }
}
