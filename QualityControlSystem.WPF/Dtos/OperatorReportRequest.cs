using System.Collections.Generic;

namespace QualityControlSystem.WPF.Dtos;

public sealed class OperatorReportRequest
{
    public IReadOnlyCollection<int> FrameIds { get; init; } = [];
    public IReadOnlyCollection<EdgeInspectionResultDto> Results { get; init; } = [];
    public string? OutputPath { get; init; }
}
