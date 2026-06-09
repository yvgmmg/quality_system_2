namespace QualityControlSystem.WPF.Dtos;

public sealed class OperatorInspectionResultSummary
{
    public int TotalCount { get; init; }
    public int PassedCount { get; init; }
    public int FailedCount => TotalCount - PassedCount;
    public bool HasResults => TotalCount > 0;
}
