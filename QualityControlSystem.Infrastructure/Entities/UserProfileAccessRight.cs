using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QualityControlSystem.Infrastructure.Entities;

[Table("user_profile_access_rights")]
public partial class UserProfileAccessRight
{
    [Key]
    [Column("user_profile_access_rights_id")]
    public int UserProfileAccessRightsId { get; set; }

    [Column("user_profile_id")]
    public int UserProfileId { get; set; }

    [Column("access_right_id")]
    public int? AccessRightId { get; set; }

    [ForeignKey("AccessRightId")]
    [InverseProperty("UserProfileAccessRights")]
    public virtual AccessRight? AccessRight { get; set; }

    [ForeignKey("UserProfileId")]
    [InverseProperty("UserProfileAccessRights")]
    public virtual UserProfile UserProfile { get; set; } = null!;
}
