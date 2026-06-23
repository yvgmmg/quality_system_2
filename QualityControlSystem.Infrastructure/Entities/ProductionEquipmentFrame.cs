using System.ComponentModel.DataAnnotations.Schema;

namespace QualityControlSystem.Infrastructure.Entities;

[Table("production_equipment_frame")]
public partial class ProductionEquipmentFrame
{
    [Column("production_equipment_id")]
    public int ProductionEquipmentId { get; set; }

    [Column("frame_id")]
    public int FrameId { get; set; }

    [ForeignKey("FrameId")]
    [InverseProperty("ProductionEquipmentFrames")]
    public virtual Frame Frame { get; set; } = null!;

    [ForeignKey("ProductionEquipmentId")]
    [InverseProperty("ProductionEquipmentFrames")]
    public virtual ProductionEquipment ProductionEquipment { get; set; } = null!;
}
