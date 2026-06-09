using QualityControlSystem.WPF.Dtos;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace QualityControlSystem.WPF.Services.Interfaces;

public interface IOperatorInspectionSessionService
{
    Task<IReadOnlyList<LookupItemDto>> GetFrameOptionsAsync(CancellationToken cancellationToken = default);
    Task<string> CheckConnectionAsync(CancellationToken cancellationToken = default);
    Task<string> DeployScriptsAsync(CancellationToken cancellationToken = default);
    Task<string> StartPhotomakerAsync(CancellationToken cancellationToken = default);
    Task StopPhotomakerAsync(CancellationToken cancellationToken = default);
    Task<int> CaptureTemplateForFrameAsync(int frameId, string side, CancellationToken cancellationToken = default);
    Task<string> SyncTemplatesAsync(CancellationToken cancellationToken = default);
    Task<string> StartOperatingForFramesAsync(IEnumerable<int> frameIds, CancellationToken cancellationToken = default);
    Task StopOperatingAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EdgeInspectionResultDto>> GetInspectionResultsAsync(CancellationToken cancellationToken = default);
}
