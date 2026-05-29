using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QualityControlSystem.Infrastructure.Entities
{
    [Table("frame_test_form_params")]
    public class FrameTestFormParams
    {
        [Key]
        [Column("frame_test_form_params_id")]
        public int Id { get; set; }

        [Column("frame_test_form_id")]
        public int? FrameTestFormId { get; set; }
        public virtual FrameTestForm? FrameTestForm { get; set; }

        [Column("params_id")]
        public int? ParamsId { get; set; }
        public virtual Params? Params { get; set; }
    }
}
