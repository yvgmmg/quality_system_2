using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QualityControlSystem.Infrastructure.Entities
{
    [Table("user_profile_access_rights")]
    public class UserProfileAccessRights
    {
        [Key]
        [Column("user_profile_access_rights_id")]
        public int Id { get; set; }

        [Column("user_profile_id")]
        public int UserProfileId { get; set; }
        public virtual UserProfile UserProfile { get; set; } = null!;

        [Column("access_right_id")]
        public int? AccessRightId { get; set; }
        public virtual AccessRight? AccessRight { get; set; }
    }
}
