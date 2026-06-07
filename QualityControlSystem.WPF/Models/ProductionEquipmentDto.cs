namespace QualityControlSystem.WPF.Models;

public class ProductionEquipmentDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public string OkofCode { get; set; } = string.Empty;
    public string InventoryNumber { get; set; } = string.Empty;
    public int WorkshopId { get; set; }
    public string WorkshopName { get; set; } = string.Empty;
}
