using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QualityControlSystem.Infrastructure.Entities
{
    [Table("frame_test_form_frame")]
    public class FrameTestFormFrame
    {
        [Key]
        [Column("frame_test_form_frame_id")]
        public int Id { get; set; }

        [Column("frame_id")]
        public int? FrameId { get; set; }
        public virtual Frame? Frame { get; set; }

        [Column("frame_test_form_id")]
        public int? FrameTestFormId { get; set; }
        public virtual FrameTestForm? FrameTestForm { get; set; }
    }
}
