using QualityControlSystem.WPF.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QualityControlSystem.WPF.Services.Interfaces;

public interface IQualityTestService
{
    Task<IReadOnlyList<QualityTestDto>> GetTestsAsync();
    Task<IReadOnlyList<LookupItemDto>> GetFramesAsync();
    Task<IReadOnlyList<TemplateDto>> GetTemplatesAsync();
    Task AddTestAsync(QualityTestDto test);
    Task UpdateTestAsync(QualityTestDto test);
    Task DeleteTestAsync(int id);
}
