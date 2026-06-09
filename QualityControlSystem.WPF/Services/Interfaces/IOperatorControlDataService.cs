using QualityControlSystem.WPF.Dtos;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace QualityControlSystem.WPF.Services.Interfaces;

public interface IOperatorControlDataService
{
    Task<IReadOnlyDictionary<int, OperatorFrameInspectionInfoDto>> GetTemplateFrameMapAsync(
        IReadOnlyCollection<int> frameIds,
        CancellationToken cancellationToken = default);
}
