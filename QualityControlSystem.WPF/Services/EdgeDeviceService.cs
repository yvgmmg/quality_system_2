using QualityControlSystem.WPF.Models;
using QualityControlSystem.WPF.Services.Interfaces;
using System;
using System.Threading.Tasks;

namespace QualityControlSystem.WPF.Services
{
    public class EdgeDeviceService : IEdgeDeviceService
    {
        private readonly IApiClientService _apiClient;
        private readonly INotificationService _notificationService;

        public EdgeDeviceService(IApiClientService apiClient, INotificationService notificationService)
        {
            _apiClient = apiClient;
            _notificationService = notificationService;
        }

        public async Task<AnalysisResultDto> AnalyzeFrameAsync()
        {
            try
            {
                var result = await _apiClient.PostAsync<object, AnalysisResultDto>("/api/analyze", null);
                _notificationService.ShowSuccess("Анализ выполнен успешно");
                return result;
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Ошибка анализа: {ex.Message}");
                throw;
            }
        }

        public async Task CaptureTemplateAsync(int templateNumber)
        {
            try
            {
                await _apiClient.PostAsync<object>($"/api/capture-template?number={templateNumber}");
                _notificationService.ShowSuccess($"Шаблон №{templateNumber} успешно захвачен");
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Ошибка захвата шаблона: {ex.Message}");
                throw;
            }
        }

        public async Task<DeviceStatusDto> GetStatusAsync()
        {
            try
            {
                return await _apiClient.GetAsync<DeviceStatusDto>("/api/status");
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Ошибка получения статуса: {ex.Message}");
                throw;
            }
        }
    }
}