using System;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QualityControlSystem.WPF.Constants;
using QualityControlSystem.WPF.Dtos;
using QualityControlSystem.WPF.Services.Interfaces;
using QualityControlSystem.WPF.Validation;
using QualityControlSystem.WPF.ViewModels.Base;

namespace QualityControlSystem.WPF.ViewModels;

public partial class EquipmentManagementViewModel : BaseViewModel
{
    private const string AllWorkshopsFilter = UiFilterOptions.AllWorkshops;

    private readonly IEquipmentManagementService _equipmentService;
    private readonly IEquipmentValidator _equipmentValidator;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private ObservableCollection<ProductionEquipmentDto> _equipment = new();

    [ObservableProperty]
    private ICollectionView? _equipmentView;

    [ObservableProperty]
    private ObservableCollection<LookupItemDto> _workshopOptions = new();

    [ObservableProperty]
    private ObservableCollection<string> _workshopFilterOptions = new();

    [ObservableProperty]
    private ObservableCollection<SelectableLookupItemDto> _frameOptions = new();

    [ObservableProperty]
    private ObservableCollection<SelectableLookupItemDto> _sensorOptions = new();

    [ObservableProperty]
    private ProductionEquipmentDto? _selectedEquipment;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _selectedWorkshopFilter = AllWorkshopsFilter;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    public EquipmentManagementViewModel(
        IEquipmentManagementService equipmentService,
        IEquipmentValidator equipmentValidator,
        IDialogService dialogService)
    {
        _equipmentService = equipmentService;
        _equipmentValidator = equipmentValidator;
        _dialogService = dialogService;
        EquipmentView = CollectionViewSource.GetDefaultView(Equipment);
        EquipmentView.Filter = FilterEquipment;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        IsBusy = true;
        StatusMessage = "Загрузка оборудования...";

        try
        {
            await LoadWorkshopOptionsAsync();
            await LoadEquipmentAsync();
            await LoadLinkOptionsAsync();
            StatusMessage = Equipment.Count == 0
                ? "Оборудование не найдено."
                : $"Оборудования: {Equipment.Count}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка загрузки оборудования: {GetErrorMessage(ex)}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadWorkshopOptionsAsync()
    {
        var workshops = await _equipmentService.GetWorkshopOptionsAsync();

        WorkshopOptions.Clear();
        foreach (var workshop in workshops)
            WorkshopOptions.Add(workshop);

        WorkshopFilterOptions.Clear();
        WorkshopFilterOptions.Add(AllWorkshopsFilter);
        foreach (var workshop in workshops
            .Select(item => item.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct())
        {
            WorkshopFilterOptions.Add(workshop);
        }

        if (!WorkshopFilterOptions.Contains(SelectedWorkshopFilter))
            SelectedWorkshopFilter = AllWorkshopsFilter;
    }

    private async Task LoadEquipmentAsync()
    {
        var selectedId = SelectedEquipment?.Id;
        var rows = await _equipmentService.GetEquipmentAsync();

        Equipment.Clear();
        foreach (var item in rows)
            Equipment.Add(item);

        SelectedEquipment = Equipment.FirstOrDefault(item => item.Id == selectedId);
        EquipmentView?.Refresh();
    }

    private async Task LoadLinkOptionsAsync()
    {
        await LoadFrameLinksAsync();
        await LoadSensorLinksAsync();
    }

    private async Task LoadFrameLinksAsync()
    {
        FrameOptions.Clear();

        if (SelectedEquipment == null)
            return;

        var linked = await _equipmentService.GetFramesForEquipmentAsync(SelectedEquipment.Id);
        var linkedIds = linked.Where(item => item.Id.HasValue).Select(item => item.Id!.Value).ToHashSet();
        var frames = await _equipmentService.GetAvailableFramesAsync();

        foreach (var frame in frames.Where(item => item.Id.HasValue))
        {
            FrameOptions.Add(new SelectableLookupItemDto
            {
                Id = frame.Id!.Value,
                Name = frame.Name,
                IsSelected = linkedIds.Contains(frame.Id.Value)
            });
        }
    }

    private async Task LoadSensorLinksAsync()
    {
        SensorOptions.Clear();

        if (SelectedEquipment == null)
            return;

        var linked = await _equipmentService.GetSensorsForEquipmentAsync(SelectedEquipment.Id);
        var linkedIds = linked.Select(item => item.Id).ToHashSet();
        var sensors = await _equipmentService.GetAvailableEquipmentSensorsAsync();

        foreach (var sensor in sensors)
        {
            SensorOptions.Add(new SelectableLookupItemDto
            {
                Id = sensor.Id,
                Name = $"{sensor.Name} ({sensor.Code})",
                IsSelected = linkedIds.Contains(sensor.Id)
            });
        }
    }

    [RelayCommand]
    private async Task AddEquipmentAsync()
    {
        var newEquipment = new ProductionEquipmentDto
        {
            WorkshopId = WorkshopOptions.Count == 1 && WorkshopOptions[0].Id is int workshopId
                ? workshopId
                : 0
        };
        if (!_dialogService.ShowProductionEquipmentDialog(newEquipment, WorkshopOptions, false))
            return;

        await RunBusyAsync(async () =>
        {
            EnsureEquipmentIsValid(newEquipment);
            await _equipmentService.AddEquipmentAsync(newEquipment);
            await LoadEquipmentAsync();
            StatusMessage = "Оборудование добавлено.";
        });
    }

    [RelayCommand]
    private async Task EditEquipmentAsync()
    {
        if (SelectedEquipment == null)
        {
            StatusMessage = "Выберите оборудование.";
            return;
        }

        var editEquipment = new ProductionEquipmentDto
        {
            Id = SelectedEquipment.Id,
            Name = SelectedEquipment.Name,
            SerialNumber = SelectedEquipment.SerialNumber,
            OkofCode = SelectedEquipment.OkofCode,
            InventoryNumber = SelectedEquipment.InventoryNumber,
            WorkshopId = SelectedEquipment.WorkshopId
        };

        if (!_dialogService.ShowProductionEquipmentDialog(editEquipment, WorkshopOptions, true))
            return;

        await RunBusyAsync(async () =>
        {
            EnsureEquipmentIsValid(editEquipment);
            await _equipmentService.UpdateEquipmentAsync(editEquipment);
            await LoadEquipmentAsync();
            StatusMessage = "Оборудование обновлено.";
        });
    }

    [RelayCommand]
    private async Task DeleteEquipmentAsync()
    {
        if (SelectedEquipment == null)
        {
            StatusMessage = "Выберите оборудование.";
            return;
        }

        if (!_dialogService.ShowConfirm($"Удалить оборудование \"{SelectedEquipment.Name}\"?"))
            return;

        await RunBusyAsync(async () =>
        {
            await _equipmentService.DeleteEquipmentAsync(SelectedEquipment.Id);
            SelectedEquipment = null;
            await LoadEquipmentAsync();
            await LoadLinkOptionsAsync();
            StatusMessage = "Оборудование удалено.";
        });
    }

    [RelayCommand]
    private async Task SaveFrameLinksAsync()
    {
        if (SelectedEquipment == null)
        {
            StatusMessage = "Выберите оборудование.";
            return;
        }

        await RunBusyAsync(async () =>
        {
            var frameIds = FrameOptions.Where(item => item.IsSelected).Select(item => item.Id).ToList();
            await _equipmentService.UpdateFramesForEquipmentAsync(SelectedEquipment.Id, frameIds);
            StatusMessage = "Связи оборудования с каркасами обновлены.";
        });
    }

    [RelayCommand]
    private async Task SaveSensorLinksAsync()
    {
        if (SelectedEquipment == null)
        {
            StatusMessage = "Выберите оборудование.";
            return;
        }

        await RunBusyAsync(async () =>
        {
            var sensorIds = SensorOptions.Where(item => item.IsSelected).Select(item => item.Id).ToList();
            await _equipmentService.UpdateSensorsForEquipmentAsync(SelectedEquipment.Id, sensorIds);
            StatusMessage = "Связи оборудования с датчиками обновлены.";
        });
    }

    private async Task RunBusyAsync(Func<Task> action)
    {
        if (IsBusy)
            return;

        IsBusy = true;
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка: {GetErrorMessage(ex)}";
            _dialogService.ShowMessage(StatusMessage, "Ошибка");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void EnsureEquipmentIsValid(ProductionEquipmentDto equipment)
    {
        var result = _equipmentValidator.Validate(equipment);
        if (!result.IsValid)
            throw new InvalidOperationException(result.ErrorMessage ?? "Данные оборудования заполнены некорректно.");
    }

    partial void OnSelectedEquipmentChanged(ProductionEquipmentDto? value)
    {
        _ = LoadLinkOptionsAsync();
    }

    partial void OnSearchTextChanged(string value) => EquipmentView?.Refresh();

    partial void OnSelectedWorkshopFilterChanged(string value) => EquipmentView?.Refresh();

    private bool FilterEquipment(object item)
    {
        if (item is not ProductionEquipmentDto equipment)
            return false;

        var search = SearchText.Trim();
        if (!string.IsNullOrWhiteSpace(search)
            && !Contains(equipment.Name, search)
            && !Contains(equipment.SerialNumber, search)
            && !Contains(equipment.OkofCode, search)
            && !Contains(equipment.InventoryNumber, search)
            && !Contains(equipment.WorkshopName, search))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(SelectedWorkshopFilter)
            && SelectedWorkshopFilter != AllWorkshopsFilter
            && !string.Equals(equipment.WorkshopName, SelectedWorkshopFilter, StringComparison.OrdinalIgnoreCase))
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

    private static string GetErrorMessage(Exception exception)
    {
        var current = exception;
        while (current.InnerException != null)
            current = current.InnerException;

        return current.Message;
    }
}
