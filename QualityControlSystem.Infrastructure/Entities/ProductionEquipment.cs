using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QualityControlSystem.Infrastructure.Entities;

[Table("production_equipment")]
public partial class ProductionEquipment
{
    [Key]
    [Column("production_equipment_id")]
    public int ProductionEquipmentId { get; set; }

    [Column("name")]
    [StringLength(255)]
    public string Name { get; set; } = null!;

    [Column("serial_number")]
    [StringLength(20)]
    public string? SerialNumber { get; set; }

    [Column("okof_code")]
    [StringLength(19)]
    public string OkofCode { get; set; } = null!;

    [Column("inventory_number")]
    [StringLength(17)]
    public string InventoryNumber { get; set; } = null!;

    [Column("workshop_id")]
    public int WorkshopId { get; set; }

    [InverseProperty("ProductionEquipment")]
    public virtual ICollection<CheckNotification> CheckNotifications { get; set; } = new List<CheckNotification>();

    [InverseProperty("ProductionEquipment")]
    public virtual ICollection<ProductionEquipmentFrame> ProductionEquipmentFrames { get; set; } = new List<ProductionEquipmentFrame>();

    [InverseProperty("ProductionEquipment")]
    public virtual ICollection<Sensor> Sensors { get; set; } = new List<Sensor>();

    [ForeignKey("WorkshopId")]
    [InverseProperty("ProductionEquipments")]
    public virtual Workshop Workshop { get; set; } = null!;
}
