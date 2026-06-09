using QualityControlSystem.WPF.Constants;
using QualityControlSystem.WPF.Dtos;
using QualityControlSystem.WPF.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QualityControlSystem.WPF.Services;

public sealed class OperatorInspectionResultService : IOperatorInspectionResultService
{
    public IReadOnlyList<EdgeInspectionResultDto> BuildItems(
        IEnumerable<EdgeInspectionResultDto> results,
        IReadOnlyDictionary<int, OperatorFrameInspectionInfoDto> templateFrameMap,
        OperatorFrameInspectionInfoDto? fallbackFrame)
    {
        var chronological = results
            .Where(result => result.RecordedAt != default)
            .GroupBy(result => new { result.RecordedAt, result.Id })
            .Select(group => group.First())
            .OrderBy(result => result.RecordedAt)
            .ThenBy(result => result.Id)
            .ToList();

        for (var index = 0; index < chronological.Count; index++)
        {
            var result = chronological[index];
            result.ControlNumber = index + 1;
            ApplyFrameInfo(result, templateFrameMap, fallbackFrame);
        }

        return chronological
            .OrderByDescending(result => result.RecordedAt)
            .ThenByDescending(result => result.Id)
            .ToList();
    }

    public OperatorInspectionResultSummary BuildSummary(IEnumerable<EdgeInspectionResultDto> results)
    {
        var items = results.ToList();
        return new OperatorInspectionResultSummary
        {
            TotalCount = items.Count,
            PassedCount = items.Count(IsPassedResult)
        };
    }

    private static void ApplyFrameInfo(
        EdgeInspectionResultDto result,
        IReadOnlyDictionary<int, OperatorFrameInspectionInfoDto> templateFrameMap,
        OperatorFrameInspectionInfoDto? fallbackFrame)
    {
        if (templateFrameMap.TryGetValue(result.TemplateId, out var frameInfo))
        {
            result.FrameId = frameInfo.FrameId;
            result.FrameName = frameInfo.FrameName;
            result.ExpectedWeight = frameInfo.ExpectedWeight;
        }
        else if (fallbackFrame is not null)
        {
            result.FrameId = fallbackFrame.FrameId;
            result.FrameName = fallbackFrame.FrameName;
            result.ExpectedWeight = fallbackFrame.ExpectedWeight;
        }

        result.WeightTolerance = result.ExpectedWeight.HasValue
            ? EdgeInspectionResultDto.GetDefaultWeightTolerance(result.ExpectedWeight.Value)
            : null;
    }

    private static bool IsPassedResult(EdgeInspectionResultDto result)
    {
        if (!IsPassedStatusForQualityResult(result.Status))
            return false;

        if (!result.ExpectedWeight.HasValue || !result.Weight.HasValue)
            return false;

        var tolerance = result.WeightTolerance ?? EdgeInspectionResultDto.GetDefaultWeightTolerance(result.ExpectedWeight.Value);
        return Math.Abs(result.Weight.Value - result.ExpectedWeight.Value) <= tolerance;
    }

    private static bool IsPassedStatusForQualityResult(string? status)
    {
        return string.Equals(status, InspectionStatuses.Ok, StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, InspectionStatuses.AcceptedRu, StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, InspectionStatuses.Passed, StringComparison.OrdinalIgnoreCase);
    }
}
