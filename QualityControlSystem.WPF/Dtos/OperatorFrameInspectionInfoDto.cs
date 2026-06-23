namespace QualityControlSystem.WPF.Dtos;

public sealed class OperatorFrameInspectionInfoDto
{
    public int FrameId { get; init; }
    public string FrameName { get; init; } = string.Empty;
    public double? ExpectedWeight { get; init; }
}
