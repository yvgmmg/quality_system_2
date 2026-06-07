using QualityControlSystem.WPF.Dtos;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace QualityControlSystem.WPF.Services.Interfaces
{
    public interface IEdgeDeviceService
    {
        Task<AnalysisResultDto> AnalyzeFrameAsync();
        Task CaptureTemplateAsync(int templateNumber);
        Task<DeviceStatusDto> GetStatusAsync();
        string GetPhotomakerFrameUrl();
        string GetOperatingFrameUrl();
        Task<IReadOnlyList<LookupItemDto>> GetFrameOptionsAsync(CancellationToken cancellationToken = default);
        Task<string> CheckConnectionAsync(CancellationToken cancellationToken = default);
        Task<string> DeployScriptsAsync(CancellationToken cancellationToken = default);
        Task<string> StartPhotomakerAsync(CancellationToken cancellationToken = default);
        Task StopPhotomakerAsync(CancellationToken cancellationToken = default);
        Task CaptureTemplateRemoteAsync(CancellationToken cancellationToken = default);
        Task<int> CaptureTemplateForFrameAsync(int frameId, string side, CancellationToken cancellationToken = default);
        Task<string> SyncTemplatesAsync(CancellationToken cancellationToken = default);
        Task<string> StartOperatingAsync(CancellationToken cancellationToken = default);
        Task<string> StartOperatingForFrameAsync(int frameId, CancellationToken cancellationToken = default);
        Task StopOperatingAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<EdgeInspectionResultDto>> GetInspectionResultsAsync(CancellationToken cancellationToken = default);
        Task<string> CreateQualityReportAsync(int frameId, IEnumerable<EdgeInspectionResultDto> results, string outputPath, CancellationToken cancellationToken = default);
    }
}
