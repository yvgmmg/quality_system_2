namespace QualityControlSystem.WPF.Dtos;

public class EquipmentSensorDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Unit { get; set; }
}
