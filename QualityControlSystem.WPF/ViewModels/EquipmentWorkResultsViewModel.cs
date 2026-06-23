using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using QualityControlSystem.WPF.Dtos;
using QualityControlSystem.WPF.Services.Interfaces;
using QualityControlSystem.WPF.ViewModels.Base;

namespace QualityControlSystem.WPF.ViewModels;

public partial class EquipmentWorkResultsViewModel : BaseViewModel, IAsyncInitializable
{
    private readonly IEquipmentWorkResultsService _resultsService;
    private readonly INotificationService _notificationService;
    private readonly IDialogService _dialogService;
    private bool _isInitialized;

    public ObservableCollection<EquipmentWorkResultDto> Results { get; } = new();

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    public EquipmentWorkResultsViewModel(
        IEquipmentWorkResultsService resultsService,
        INotificationService notificationService,
        IDialogService dialogService)
    {
        _resultsService = resultsService;
        _notificationService = notificationService;
        _dialogService = dialogService;
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

    [RelayCommand]
    private async Task ClearNotificationsAsync()
    {
        if (IsBusy)
            return;

        if (Results.Count == 0)
        {
            StatusMessage = "Список уведомлений уже пуст.";
            _notificationService.ShowWarning(StatusMessage);
            return;
        }

        if (!_dialogService.ShowConfirm("Очистить список уведомлений о результатах работы оборудования?"))
            return;

        IsBusy = true;
        try
        {
            var removedCount = await _resultsService.ClearResultsForEquipmentSpecialistAsync();
            Results.Clear();

            StatusMessage = removedCount == 0
                ? "Список уведомлений уже пуст."
                : $"Удалено уведомлений: {removedCount}";

            _notificationService.ShowSuccess(StatusMessage);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка очистки уведомлений: {GetErrorMessage(ex)}";
            _notificationService.ShowError(StatusMessage);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ExportReportAsync()
    {
        if (IsBusy)
            return;

        if (Results.Count == 0)
        {
            StatusMessage = "Нет уведомлений для формирования отчета.";
            _notificationService.ShowWarning(StatusMessage);
            return;
        }

        var dialog = new SaveFileDialog
        {
            FileName = $"equipment_work_results_report_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
            Filter = "Текстовый отчет (*.txt)|*.txt|Все файлы (*.*)|*.*",
            DefaultExt = ".txt",
            AddExtension = true,
            OverwritePrompt = true
        };

        if (dialog.ShowDialog() != true)
            return;

        IsBusy = true;
        try
        {
            await _resultsService.CreateEquipmentWorkResultsReportAsync(dialog.FileName);
            StatusMessage = $"Отчет сохранен: {dialog.FileName}";
            _notificationService.ShowSuccess("Отчет о результатах работы оборудования сохранен.");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка формирования отчета: {GetErrorMessage(ex)}";
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
