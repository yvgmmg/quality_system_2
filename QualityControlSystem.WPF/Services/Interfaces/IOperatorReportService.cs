using QualityControlSystem.WPF.Dtos;
using System.Threading;
using System.Threading.Tasks;

namespace QualityControlSystem.WPF.Services.Interfaces;

public interface IOperatorReportService
{
    Task<string> CreateQualityReportAsync(
        OperatorReportRequest request,
        CancellationToken cancellationToken = default);
}
