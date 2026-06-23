using QualityControlSystem.WPF.Dtos;
using QualityControlSystem.WPF.Services.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace QualityControlSystem.WPF.Services;

public sealed class OperatorReportService : IOperatorReportService
{
    private readonly IEdgeDeviceService _edgeDeviceService;

    public OperatorReportService(IEdgeDeviceService edgeDeviceService)
    {
        _edgeDeviceService = edgeDeviceService;
    }

    public Task<string> CreateQualityReportAsync(
        OperatorReportRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.FrameIds.Count == 0)
            throw new InvalidOperationException("Выберите модель каркаса для отчета.");

        if (request.Results.Count == 0)
            throw new InvalidOperationException("Нет результатов контроля для отчета.");

        if (string.IsNullOrWhiteSpace(request.OutputPath))
            throw new InvalidOperationException("Укажите путь для сохранения отчета.");

        return _edgeDeviceService.CreateQualityReportAsync(
            request.FrameIds,
            request.Results,
            request.OutputPath,
            cancellationToken);
    }
}
