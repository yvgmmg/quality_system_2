namespace QualityControlSystem.WPF.Dtos;

public class EquipmentWorkResultDto
{
    public int Id { get; set; }
    public int EquipmentId { get; set; }
    public string EquipmentName { get; set; } = string.Empty;
    public string? InventoryNumber { get; set; }
    public string? SerialNumber { get; set; }
    public string? OkofCode { get; set; }
    public DateTime CheckedAt { get; set; }
    public decimal? DefectPercentage { get; set; }
    public List<EquipmentSensorValueDto> SensorValues { get; set; } = new();
    public bool HasSensorValues => SensorValues.Count > 0;
}

public class EquipmentSensorValueDto
{
    public int? SensorId { get; set; }
    public string SensorName { get; set; } = "Значение датчика";
    public string SensorCode { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? Unit { get; set; }
    public DateTime? MeasuredAt { get; set; }
}
