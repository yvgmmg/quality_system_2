using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QualityControlSystem.WPF.Models
{
    public class DeviceStatusDto
    {
        public bool IsConnected { get; set; }
        public string Status { get; set; } = string.Empty;
        public int TemplatesCount { get; set; }
    }
}