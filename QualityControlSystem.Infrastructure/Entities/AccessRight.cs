using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QualityControlSystem.Infrastructure.Entities;

[Table("access_rights")]
public partial class AccessRight
{
    [Key]
    [Column("access_right_id")]
    public int AccessRightId { get; set; }

    [Column("access_right", TypeName = "character varying")]
    public string? AccessRight1 { get; set; }

    [InverseProperty("AccessRight")]
    public virtual ICollection<UserProfileAccessRight> UserProfileAccessRights { get; set; } = new List<UserProfileAccessRight>();
}
