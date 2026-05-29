using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QualityControlSystem.Infrastructure.Entities
{
    [Table("camera_frame")]
    public class CameraFrame
    {
        [Key]
        [Column("camera_frame_id")]
        public int CameraFrameId { get; set; }

        [Column("path_to_camera_frame")]
        public string PathToCameraFrame { get; set; } = null!;

        [Column("path_to_processed_camera_frame")]
        public string PathToProcessedCameraFrame { get; set; } = null!;

        [Column("record_time")]
        public DateTime RecordTime { get; set; }

        [Column("camera_id")]
        public int? CameraId { get; set; }
        public virtual Camera? Camera { get; set; }

        [Column("frame_id")]
        public int? FrameId { get; set; }
        public virtual Frame? Frame { get; set; }
    }
}
