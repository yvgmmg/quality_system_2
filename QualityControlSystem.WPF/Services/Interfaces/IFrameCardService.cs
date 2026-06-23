using QualityControlSystem.WPF.Dtos;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace QualityControlSystem.WPF.Services.Interfaces;

public interface IFrameCardService
{
    int? CurrentWorkshopId { get; }
    Task<IReadOnlyList<FrameCardDto>> GetFramesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LookupItemDto>> GetMaterialTypesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LookupItemDto>> GetWorkshopsAsync(CancellationToken cancellationToken = default);
    Task AddFrameAsync(FrameCardDto frame, CancellationToken cancellationToken = default);
    Task UpdateFrameAsync(FrameCardDto frame, CancellationToken cancellationToken = default);
    Task DeleteFrameAsync(int frameId, CancellationToken cancellationToken = default);
}
