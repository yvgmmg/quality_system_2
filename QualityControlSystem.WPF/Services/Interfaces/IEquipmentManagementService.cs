using QualityControlSystem.WPF.Dtos;

namespace QualityControlSystem.WPF.Services.Interfaces;

public interface IEquipmentManagementService
{
    Task<IReadOnlyList<LookupItemDto>> GetWorkshopOptionsAsync();
    Task<IReadOnlyList<ProductionEquipmentDto>> GetEquipmentAsync();
    Task AddEquipmentAsync(ProductionEquipmentDto equipment);
    Task UpdateEquipmentAsync(ProductionEquipmentDto equipment);
    Task DeleteEquipmentAsync(int equipmentId);
    Task<IReadOnlyList<LookupItemDto>> GetAvailableFramesAsync();
    Task<IReadOnlyList<LookupItemDto>> GetFramesForEquipmentAsync(int equipmentId);
    Task UpdateFramesForEquipmentAsync(int equipmentId, IReadOnlyCollection<int> frameIds);
    Task<IReadOnlyList<EquipmentSensorDto>> GetAvailableEquipmentSensorsAsync();
    Task<IReadOnlyList<EquipmentSensorDto>> GetSensorsForEquipmentAsync(int equipmentId);
    Task UpdateSensorsForEquipmentAsync(int equipmentId, IReadOnlyCollection<int> sensorIds);
}
