using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QualityControlSystem.Infrastructure.Entities;

[Table("camera")]
public partial class Camera
{
    [Column("inventory_number")]
    [StringLength(14)]
    public string? InventoryNumber { get; set; }

    [Column("name")]
    [StringLength(255)]
    public string? Name { get; set; }

    [Column("workshop_id")]
    public int? WorkshopId { get; set; }

    [Key]
    [Column("camera_id")]
    public int CameraId { get; set; }

    [InverseProperty("Camera")]
    public virtual ICollection<CameraFrame> CameraFrames { get; set; } = new List<CameraFrame>();

    [ForeignKey("WorkshopId")]
    [InverseProperty("Cameras")]
    public virtual Workshop? Workshop { get; set; }
}
