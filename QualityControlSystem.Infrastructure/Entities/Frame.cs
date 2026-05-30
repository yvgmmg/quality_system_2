using Microsoft.EntityFrameworkCore;
using QualityControlSystem.Infrastructure.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QualityControlSystem.Infrastructure.Entities;

[Table("frame")]
public partial class Frame
{
    [Key]
    [Column("frame_id")]
    public int FrameId { get; set; }

    [Column("production_number")]
    [StringLength(255)]
    public string ProductionNumber { get; set; } = null!;

    [Column("instruction_id")]
    public int? InstructionId { get; set; }

    [Column("batch_id")]
    public int? BatchId { get; set; }

    [Column("workshop_id")]
    public int? WorkshopId { get; set; }

    [ForeignKey("BatchId")]
    [InverseProperty("Frames")]
    public virtual Batch? Batch { get; set; }

    [InverseProperty("Frame")]
    public virtual ICollection<CameraFrame> CameraFrames { get; set; } = new List<CameraFrame>();

    [InverseProperty("Frame")]
    public virtual ICollection<FrameTestFormFrame> FrameTestFormFrames { get; set; } = new List<FrameTestFormFrame>();

    [ForeignKey("InstructionId")]
    [InverseProperty("Frames")]
    public virtual Instruction? Instruction { get; set; }

    [InverseProperty("Frame")]
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    [InverseProperty("Frame")]
    public virtual ICollection<SensorReading> SensorReadings { get; set; } = new List<SensorReading>();

    [ForeignKey("WorkshopId")]
    [InverseProperty("Frames")]
    public virtual Workshop? Workshop { get; set; }

    [Column("result")]
    public FrameResult? Result { get; set; }
}
