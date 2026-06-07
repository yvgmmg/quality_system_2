using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QualityControlSystem.Infrastructure.Entities;

[Table("camera")]
public partial class Camera
{
    [Key]
    [Column("camera_id")]
    public int CameraId { get; set; }

    [Column("okof_code")]
    [StringLength(19)]
    public string OkofCode { get; set; } = null!;

    [Column("inventory_number")]
    [StringLength(17)]
    public string InventoryNumber { get; set; } = null!;

    [Column("name")]
    [StringLength(255)]
    public string Name { get; set; } = null!;

    [Column("workshop_id")]
    public int WorkshopId { get; set; }

    [ForeignKey("WorkshopId")]
    [InverseProperty("Cameras")]
    public virtual Workshop Workshop { get; set; } = null!;
}
