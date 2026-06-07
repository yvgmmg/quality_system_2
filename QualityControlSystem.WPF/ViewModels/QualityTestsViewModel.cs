using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QualityControlSystem.WPF.Models;
using QualityControlSystem.WPF.Services.Interfaces;
using QualityControlSystem.WPF.ViewModels.Base;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace QualityControlSystem.WPF.ViewModels;

public partial class QualityTestsViewModel : BaseViewModel
{
    private readonly IQualityTestService _qualityTestService;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private ObservableCollection<QualityTestDto> _tests = new();

    [ObservableProperty]
    private QualityTestDto? _selectedTest;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    public QualityTestsViewModel(IQualityTestService qualityTestService, IDialogService dialogService)
    {
        _qualityTestService = qualityTestService;
        _dialogService = dialogService;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        IsBusy = true;
        StatusMessage = "Загрузка тестов контроля...";

        try
        {
            var tests = await _qualityTestService.GetTestsAsync();
            Tests.Clear();
            foreach (var test in tests)
                Tests.Add(test);

            StatusMessage = Tests.Count == 0 ? "Тесты не найдены." : $"Тестов: {Tests.Count}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка загрузки тестов: {GetErrorMessage(ex)}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task AddTestAsync()
    {
        var test = new QualityTestDto();
        if (!await ShowDialogAsync(test, false))
            return;

        await RunActionAsync(async () =>
        {
            await _qualityTestService.AddTestAsync(test);
            await LoadAsync();
            StatusMessage = "Тест добавлен.";
        }, "Не удалось добавить тест");
    }

    [RelayCommand]
    private async Task EditTestAsync()
    {
        if (SelectedTest == null)
        {
            StatusMessage = "Выберите тест.";
            return;
        }

        var edit = new QualityTestDto
        {
            Id = SelectedTest.Id,
            Name = SelectedTest.Name,
            Description = SelectedTest.Description,
            FrameId = SelectedTest.FrameId,
            TemplateIds = SelectedTest.TemplateIds.ToList()
        };

        if (!await ShowDialogAsync(edit, true))
            return;

        await RunActionAsync(async () =>
        {
            await _qualityTestService.UpdateTestAsync(edit);
            await LoadAsync();
            StatusMessage = "Тест обновлен.";
        }, "Не удалось обновить тест");
    }

    [RelayCommand]
    private async Task DeleteTestAsync()
    {
        if (SelectedTest == null)
        {
            StatusMessage = "Выберите тест.";
            return;
        }

        if (!_dialogService.ShowConfirm($"Удалить тест \"{SelectedTest.Name}\"?"))
            return;

        await RunActionAsync(async () =>
        {
            await _qualityTestService.DeleteTestAsync(SelectedTest.Id);
            SelectedTest = null;
            await LoadAsync();
            StatusMessage = "Тест удален.";
        }, "Не удалось удалить тест");
    }

    private async Task<bool> ShowDialogAsync(QualityTestDto test, bool isEdit)
    {
        var frames = await _qualityTestService.GetFramesAsync();
        var templates = await _qualityTestService.GetTemplatesAsync();

        return _dialogService.ShowQualityTestDialog(test, frames, templates, isEdit);
    }

    private async Task RunActionAsync(Func<Task> action, string errorPrefix)
    {
        IsBusy = true;
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            var message = $"{errorPrefix}: {GetErrorMessage(ex)}";
            StatusMessage = message;
            _dialogService.ShowMessage(message, "Ошибка");
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
