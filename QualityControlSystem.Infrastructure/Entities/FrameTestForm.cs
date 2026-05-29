using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QualityControlSystem.Infrastructure.Entities
{
    [Table("frame_test_form")]
    public class FrameTestForm
    {
        [Key]
        [Column("frame_test_form_id")]
        public int FrameTestFormId { get; set; }

        [Column("name")]
        public string Name { get; set; } = null!;

        [Column("result")]
        public TestResult Result { get; set; }

        [Column("comments")]
        public string? Comments { get; set; }

        [Column("user_profile_id")]
        public int? UserProfileId { get; set; }
        public virtual UserProfile? UserProfile { get; set; }
    }
}
