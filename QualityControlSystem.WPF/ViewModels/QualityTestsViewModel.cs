using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QualityControlSystem.WPF.Constants;
using QualityControlSystem.WPF.Dtos;
using QualityControlSystem.WPF.Services.Interfaces;
using QualityControlSystem.WPF.ViewModels.Base;
using System;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;

namespace QualityControlSystem.WPF.ViewModels;

public partial class QualityTestsViewModel : BaseViewModel, IAsyncInitializable
{
    private const string AllFramesFilter = UiFilterOptions.AllFrames;

    private readonly IQualityTestService _qualityTestService;
    private readonly IDialogService _dialogService;
    private bool _isInitialized;

    [ObservableProperty]
    private ObservableCollection<QualityTestDto> _tests = new();

    [ObservableProperty]
    private ICollectionView? _testsView;

    [ObservableProperty]
    private ObservableCollection<string> _frameFilterOptions = new();

    [ObservableProperty]
    private QualityTestDto? _selectedTest;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _selectedFrameFilter = AllFramesFilter;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    public QualityTestsViewModel(IQualityTestService qualityTestService, IDialogService dialogService)
    {
        _qualityTestService = qualityTestService;
        _dialogService = dialogService;
        TestsView = CollectionViewSource.GetDefaultView(Tests);
        TestsView.Filter = FilterTest;
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized)
            return;

        _isInitialized = true;
        await LoadAsync();
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

            RebuildFrameFilters();
            TestsView?.Refresh();

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

    private void RebuildFrameFilters()
    {
        FrameFilterOptions.Clear();
        FrameFilterOptions.Add(AllFramesFilter);
        foreach (var frame in Tests
            .Select(test => test.FrameName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct())
        {
            FrameFilterOptions.Add(frame);
        }

        if (!FrameFilterOptions.Contains(SelectedFrameFilter))
            SelectedFrameFilter = AllFramesFilter;
    }

    partial void OnSearchTextChanged(string value) => TestsView?.Refresh();

    partial void OnSelectedFrameFilterChanged(string value) => TestsView?.Refresh();

    private bool FilterTest(object item)
    {
        if (item is not QualityTestDto test)
            return false;

        var search = SearchText.Trim();
        if (!string.IsNullOrWhiteSpace(search)
            && !Contains(test.Name, search)
            && !Contains(test.Description, search)
            && !Contains(test.FrameName, search)
            && !Contains(test.TemplateSummary, search))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(SelectedFrameFilter)
            && SelectedFrameFilter != AllFramesFilter
            && !string.Equals(test.FrameName, SelectedFrameFilter, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private static bool Contains(string? value, string search)
    {
        return !string.IsNullOrWhiteSpace(value)
            && value.Contains(search, StringComparison.OrdinalIgnoreCase);
    }
}
