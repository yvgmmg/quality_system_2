using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QualityControlSystem.Infrastructure.Entities;

[Table("camera_frame")]
public partial class CameraFrame
{
    [Key]
    [Column("camera_frame_id")]
    public int CameraFrameId { get; set; }

    [Column("path_to_camera_frame")]
    [StringLength(500)]
    public string PathToCameraFrame { get; set; } = null!;

    [Column("path_to_processed_camera_frame")]
    [StringLength(500)]
    public string PathToProcessedCameraFrame { get; set; } = null!;

    [Column("record_time", TypeName = "timestamp without time zone")]
    public DateTime RecordTime { get; set; }

    [Column("camera_id")]
    public int? CameraId { get; set; }

    [Column("frame_id")]
    public int? FrameId { get; set; }

    [ForeignKey("CameraId")]
    [InverseProperty("CameraFrames")]
    public virtual Camera? Camera { get; set; }

    [ForeignKey("FrameId")]
    [InverseProperty("CameraFrames")]
    public virtual Frame? Frame { get; set; }
}
