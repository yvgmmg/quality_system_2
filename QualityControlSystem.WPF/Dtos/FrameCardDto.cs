using System.Windows.Media;

namespace QualityControlSystem.WPF.Dtos;

public class FrameCardDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MaterialTypeId { get; set; }
    public string MaterialName { get; set; } = string.Empty;
    public int WorkshopId { get; set; }
    public string WorkshopNumber { get; set; } = string.Empty;
    public string WorkshopName { get; set; } = string.Empty;
    public decimal? Weight { get; set; }
    public decimal? Length { get; set; }
    public decimal? Width { get; set; }
    public decimal? Height { get; set; }
    public string? ImagePath { get; set; }
    public ImageSource? ImagePreview { get; set; }
}
