using QualityControlSystem.WPF.Dtos;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace QualityControlSystem.WPF.Services.Interfaces;

public interface ITemplateManagementService
{
    Task<IReadOnlyList<TemplateDto>> GetTemplatesAsync(CancellationToken cancellationToken = default);
    Task UpdateTemplateAsync(TemplateDto template, CancellationToken cancellationToken = default);
    Task<string?> DeleteTemplateAsync(int templateId, CancellationToken cancellationToken = default);
}
