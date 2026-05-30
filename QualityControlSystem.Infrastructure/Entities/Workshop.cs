using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QualityControlSystem.Infrastructure.Entities;

[Table("workshop")]
public partial class Workshop
{
    [Key]
    [Column("workshop_id")]
    public int WorkshopId { get; set; }

    [Column("number")]
    public int Number { get; set; }

    [Column("appointment")]
    [StringLength(255)]
    public string Appointment { get; set; } = null!;

    [InverseProperty("Workshop")]
    public virtual ICollection<Camera> Cameras { get; set; } = new List<Camera>();

    [InverseProperty("Workshop")]
    public virtual ICollection<Frame> Frames { get; set; } = new List<Frame>();

    [InverseProperty("Workshop")]
    public virtual ICollection<ProductionEquipment> ProductionEquipments { get; set; } = new List<ProductionEquipment>();

    [InverseProperty("Workshop")]
    public virtual ICollection<Sensor> Sensors { get; set; } = new List<Sensor>();

    [InverseProperty("Workshop")]
    public virtual ICollection<UserProfile> UserProfiles { get; set; } = new List<UserProfile>();

    [InverseProperty("Workshop")]
    public virtual ICollection<WorkshopProductionOrder> WorkshopProductionOrders { get; set; } = new List<WorkshopProductionOrder>();
}
