using QualityControlSystem.WPF.Dtos;

namespace QualityControlSystem.WPF.Services.Interfaces;

public interface IEquipmentWorkResultsService
{
    Task<IReadOnlyList<EquipmentWorkResultDto>> GetResultsForEquipmentSpecialistAsync();
    Task CreateResultsForFrameInspectionAsync(int frameId, IEnumerable<EdgeInspectionResultDto> inspectionResults);
    Task<int> ClearResultsForEquipmentSpecialistAsync(CancellationToken cancellationToken = default);
    Task CreateEquipmentWorkResultsReportAsync(string outputPath, CancellationToken cancellationToken = default);
}
