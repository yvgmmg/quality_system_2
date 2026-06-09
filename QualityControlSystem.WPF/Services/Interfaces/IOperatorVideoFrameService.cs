using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace QualityControlSystem.WPF.Services.Interfaces;

public interface IOperatorVideoFrameService
{
    Task<ImageSource?> GetPhotomakerFrameAsync(CancellationToken cancellationToken = default);
    Task<ImageSource?> GetOperatingFrameAsync(CancellationToken cancellationToken = default);
}
