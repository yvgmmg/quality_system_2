using QualityControlSystem.WPF.Dtos;
using QualityControlSystem.WPF.Services.Interfaces;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace QualityControlSystem.WPF.Services;

public sealed class OperatorInspectionSessionService : IOperatorInspectionSessionService
{
    private readonly IEdgeDeviceService _edgeDeviceService;

    public OperatorInspectionSessionService(IEdgeDeviceService edgeDeviceService)
    {
        _edgeDeviceService = edgeDeviceService;
    }

    public Task<IReadOnlyList<LookupItemDto>> GetFrameOptionsAsync(CancellationToken cancellationToken = default)
    {
        return _edgeDeviceService.GetFrameOptionsAsync(cancellationToken);
    }

    public Task<string> CheckConnectionAsync(CancellationToken cancellationToken = default)
    {
        return _edgeDeviceService.CheckConnectionAsync(cancellationToken);
    }

    public Task<string> DeployScriptsAsync(CancellationToken cancellationToken = default)
    {
        return _edgeDeviceService.DeployScriptsAsync(cancellationToken);
    }

    public Task<string> StartPhotomakerAsync(CancellationToken cancellationToken = default)
    {
        return _edgeDeviceService.StartPhotomakerAsync(cancellationToken);
    }

    public Task StopPhotomakerAsync(CancellationToken cancellationToken = default)
    {
        return _edgeDeviceService.StopPhotomakerAsync(cancellationToken);
    }

    public Task<int> CaptureTemplateForFrameAsync(int frameId, string side, CancellationToken cancellationToken = default)
    {
        return _edgeDeviceService.CaptureTemplateForFrameAsync(frameId, side, cancellationToken);
    }

    public Task<string> SyncTemplatesAsync(CancellationToken cancellationToken = default)
    {
        return _edgeDeviceService.SyncTemplatesAsync(cancellationToken);
    }

    public Task<string> StartOperatingForFramesAsync(IEnumerable<int> frameIds, CancellationToken cancellationToken = default)
    {
        return _edgeDeviceService.StartOperatingForFramesAsync(frameIds, cancellationToken);
    }

    public Task StopOperatingAsync(CancellationToken cancellationToken = default)
    {
        return _edgeDeviceService.StopOperatingAsync(cancellationToken);
    }

    public Task<IReadOnlyList<EdgeInspectionResultDto>> GetInspectionResultsAsync(CancellationToken cancellationToken = default)
    {
        return _edgeDeviceService.GetInspectionResultsAsync(cancellationToken);
    }
}
