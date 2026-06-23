using QualityControlSystem.WPF.Dtos;
using System.Collections.Generic;

namespace QualityControlSystem.WPF.Services.Interfaces;

public interface IOperatorInspectionResultService
{
    IReadOnlyList<EdgeInspectionResultDto> BuildItems(
        IEnumerable<EdgeInspectionResultDto> results,
        IReadOnlyDictionary<int, OperatorFrameInspectionInfoDto> templateFrameMap,
        OperatorFrameInspectionInfoDto? fallbackFrame);

    OperatorInspectionResultSummary BuildSummary(IEnumerable<EdgeInspectionResultDto> results);
}
