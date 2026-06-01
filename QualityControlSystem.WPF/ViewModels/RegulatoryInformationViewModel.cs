using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QualityControlSystem.WPF.Models;
using QualityControlSystem.WPF.Services.Interfaces;
using QualityControlSystem.WPF.ViewModels.Base;

namespace QualityControlSystem.WPF.ViewModels;

public partial class RegulatoryInformationViewModel : BaseViewModel
{
    private readonly IRegulatoryInformationService _regulatoryInformationService;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private ObservableCollection<RegulatoryInformationDto> _regulatoryItems = new();

    [ObservableProperty]
    private RegulatoryInformationDto? _selectedItem;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string? _selectedType;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    public IReadOnlyList<FilterOption> TypeFilters { get; } =
    [
        new("Все", null),
        new("Оборудование", "equipment"),
        new("Каркас", "frame")
    ];

    public RegulatoryInformationViewModel(
        IRegulatoryInformationService regulatoryInformationService,
        IDialogService dialogService)
    {
        _regulatoryInformationService = regulatoryInformationService;
        _dialogService = dialogService;
        _ = LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await RefreshAsync(() => _regulatoryInformationService.GetAllAsync());
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        await RefreshAsync(() => _regulatoryInformationService.SearchAsync(SearchText, SelectedType));
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        var dto = new RegulatoryInformationDto
        {
            Type = "equipment",
            StartDate = DateTime.Today
        };

        if (!_dialogService.ShowRegulatoryInformationDialog(dto, false))
            return;

        try
        {
            await _regulatoryInformationService.AddAsync(dto);
            await SearchAsync();
            StatusMessage = "Запись НСИ добавлена.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Не удалось добавить запись НСИ: {GetErrorMessage(ex)}";
        }
    }

    [RelayCommand]
    private async Task EditAsync()
    {
        if (SelectedItem == null)
        {
            StatusMessage = "Выберите запись НСИ.";
            return;
        }

        var dto = Copy(SelectedItem);
        if (!_dialogService.ShowRegulatoryInformationDialog(dto, true))
            return;

        try
        {
            await _regulatoryInformationService.UpdateAsync(dto);
            await SearchAsync();
            StatusMessage = "Запись НСИ обновлена.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Не удалось обновить запись НСИ: {GetErrorMessage(ex)}";
        }
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (SelectedItem == null)
        {
            StatusMessage = "Выберите запись НСИ.";
            return;
        }

        if (!_dialogService.ShowConfirm($"Удалить запись НСИ \"{SelectedItem.Name}\"?"))
            return;

        try
        {
            await _regulatoryInformationService.DeleteAsync(SelectedItem.RegulatoryInformationId);
            SelectedItem = null;
            await SearchAsync();
            StatusMessage = "Запись НСИ удалена.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Не удалось удалить запись НСИ: {GetErrorMessage(ex)}";
        }
    }

    private async Task RefreshAsync(Func<Task<List<RegulatoryInformationDto>>> load)
    {
        IsBusy = true;
        StatusMessage = "Загрузка НСИ...";

        try
        {
            var items = await load();
            RegulatoryItems.Clear();
            foreach (var item in items)
                RegulatoryItems.Add(item);

            StatusMessage = RegulatoryItems.Count == 0 ? "Записи НСИ не найдены." : $"Записей НСИ: {RegulatoryItems.Count}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка загрузки НСИ: {GetErrorMessage(ex)}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static RegulatoryInformationDto Copy(RegulatoryInformationDto item)
    {
        return new RegulatoryInformationDto
        {
            RegulatoryInformationId = item.RegulatoryInformationId,
            Name = item.Name,
            Type = item.Type,
            Description = item.Description,
            MinValue = item.MinValue,
            MaxValue = item.MaxValue,
            Measurement = item.Measurement,
            StartDate = item.StartDate,
            EndDate = item.EndDate
        };
    }

    private static string GetErrorMessage(Exception exception)
    {
        var current = exception;
        while (current.InnerException != null)
            current = current.InnerException;

        return current.Message;
    }
}

public sealed record FilterOption(string DisplayName, string? Value);
