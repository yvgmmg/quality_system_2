using QualityControlSystem.WPF.Services.Interfaces;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace QualityControlSystem.WPF.Services;

public sealed class OperatorVideoFrameService : IOperatorVideoFrameService
{
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(3) };

    private readonly IEdgeDeviceService _edgeDeviceService;

    public OperatorVideoFrameService(IEdgeDeviceService edgeDeviceService)
    {
        _edgeDeviceService = edgeDeviceService;
    }

    public Task<ImageSource?> GetPhotomakerFrameAsync(CancellationToken cancellationToken = default)
    {
        return LoadFrameAsync(_edgeDeviceService.GetPhotomakerFrameUrl(), cancellationToken);
    }

    public Task<ImageSource?> GetOperatingFrameAsync(CancellationToken cancellationToken = default)
    {
        return LoadFrameAsync(_edgeDeviceService.GetOperatingFrameUrl(), cancellationToken);
    }

    private async Task<ImageSource?> LoadFrameAsync(string url, CancellationToken cancellationToken)
    {
        var frameUrl = $"{url}?t={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        var bytes = await HttpClient.GetByteArrayAsync(frameUrl, cancellationToken);

        using var stream = new MemoryStream(bytes);
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();

        return image;
    }
}
