using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QualityControlSystem.WPF.Dtos;
using QualityControlSystem.WPF.Services.Interfaces;
using QualityControlSystem.WPF.ViewModels.Base;

namespace QualityControlSystem.WPF.ViewModels;

public partial class EquipmentWorkResultsViewModel : BaseViewModel, IAsyncInitializable
{
    private readonly IEquipmentWorkResultsService _resultsService;
    private readonly INotificationService _notificationService;
    private bool _isInitialized;

    public ObservableCollection<EquipmentWorkResultDto> Results { get; } = new();

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    public EquipmentWorkResultsViewModel(
        IEquipmentWorkResultsService resultsService,
        INotificationService notificationService)
    {
        _resultsService = resultsService;
        _notificationService = notificationService;
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized)
            return;

        _isInitialized = true;
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;
        try
        {
            var rows = await _resultsService.GetResultsForEquipmentSpecialistAsync();
            Results.Clear();
            foreach (var row in rows)
                Results.Add(row);

            StatusMessage = Results.Count == 0
                ? "Результаты работы оборудования не найдены."
                : $"Уведомлений: {Results.Count}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка загрузки результатов: {GetErrorMessage(ex)}";
            _notificationService.ShowError(StatusMessage);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static string GetErrorMessage(Exception exception)
    {
        var current = exception;
        while (current.InnerException != null)
            current = current.InnerException;

        return current.Message;
    }
}
