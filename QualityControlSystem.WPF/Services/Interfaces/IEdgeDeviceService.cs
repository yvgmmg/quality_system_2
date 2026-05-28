using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QualityControlSystem.WPF.Models;

namespace QualityControlSystem.WPF.Services.Interfaces
{
    public interface IEdgeDeviceService
    {
        Task<AnalysisResultDto> AnalyzeFrameAsync();
        Task CaptureTemplateAsync(int templateNumber);
        Task<DeviceStatusDto> GetStatusAsync();
    }
}