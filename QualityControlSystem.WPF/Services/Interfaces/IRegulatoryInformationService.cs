using QualityControlSystem.WPF.Models;

namespace QualityControlSystem.WPF.Services.Interfaces;

public interface IRegulatoryInformationService
{
    Task<List<RegulatoryInformationDto>> GetAllAsync();
    Task<List<RegulatoryInformationDto>> SearchAsync(string? searchText, string? type);
    Task AddAsync(RegulatoryInformationDto dto);
    Task UpdateAsync(RegulatoryInformationDto dto);
    Task DeleteAsync(int id);
}
