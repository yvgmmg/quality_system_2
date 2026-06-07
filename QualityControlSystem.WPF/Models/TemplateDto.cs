using System.Windows.Media;

namespace QualityControlSystem.WPF.Models;

public class TemplateDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
    public string Side { get; set; } = string.Empty;
    public ImageSource? ImagePreview { get; set; }
}
