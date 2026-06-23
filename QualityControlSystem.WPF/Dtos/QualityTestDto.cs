using System.Collections.Generic;

namespace QualityControlSystem.WPF.Dtos;

public class QualityTestDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int FrameId { get; set; }
    public string FrameName { get; set; } = string.Empty;
    public List<int> TemplateIds { get; set; } = new();
    public string TemplateSummary { get; set; } = string.Empty;
}
