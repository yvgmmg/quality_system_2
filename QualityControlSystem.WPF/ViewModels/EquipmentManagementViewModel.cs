using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using QualityControlSystem.Infrastructure;
using QualityControlSystem.Infrastructure.Entities;
using QualityControlSystem.WPF.Models;
using QualityControlSystem.WPF.Services.Interfaces;
using QualityControlSystem.WPF.ViewModels.Base;

namespace QualityControlSystem.WPF.ViewModels;

public partial class EquipmentManagementViewModel : BaseViewModel
{
    private readonly AppDbContext _dbContext;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private ObservableCollection<ProductionEquipmentDto> _equipment = new();

    [ObservableProperty]
    private ObservableCollection<LookupItemDto> _workshopOptions = new();

    [ObservableProperty]
    private ProductionEquipmentDto? _selectedEquipment;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    public EquipmentManagementViewModel(AppDbContext dbContext, IDialogService dialogService)
    {
        _dbContext = dbContext;
        _dialogService = dialogService;
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
            StatusMessage = Equipment.Count == 0 ? "Оборудование не найдено." : $"Оборудования: {Equipment.Count}";
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
        var workshops = await _dbContext.Workshops
            .AsNoTracking()
            .OrderBy(workshop => workshop.Number)
            .Select(workshop => new LookupItemDto
            {
                Id = workshop.WorkshopId,
                Name = string.IsNullOrWhiteSpace(workshop.Purpose)
                    ? $"Цех {workshop.Number}"
                    : $"Цех {workshop.Number} - {workshop.Purpose}"
            })
            .ToListAsync();

        WorkshopOptions.Clear();
        foreach (var workshop in workshops)
            WorkshopOptions.Add(workshop);
    }

    private async Task LoadEquipmentAsync()
    {
        var selectedId = SelectedEquipment?.Id;
        var rows = await _dbContext.ProductionEquipments
            .AsNoTracking()
            .Include(item => item.Workshop)
            .OrderBy(item => item.ProductionEquipmentId)
            .Select(item => new
            {
                item.ProductionEquipmentId,
                item.Name,
                item.SerialNumber,
                item.OkofCode,
                item.InventoryNumber,
                item.WorkshopId,
                item.Workshop.Number,
                item.Workshop.Purpose
            })
            .ToListAsync();

        Equipment.Clear();
        foreach (var item in rows)
        {
            Equipment.Add(new ProductionEquipmentDto
            {
                Id = item.ProductionEquipmentId,
                Name = item.Name,
                SerialNumber = item.SerialNumber,
                OkofCode = item.OkofCode,
                InventoryNumber = item.InventoryNumber,
                WorkshopId = item.WorkshopId,
                WorkshopName = string.IsNullOrWhiteSpace(item.Purpose)
                    ? $"Цех {item.Number}"
                    : $"Цех {item.Number} - {item.Purpose}"
            });
        }

        SelectedEquipment = Equipment.FirstOrDefault(item => item.Id == selectedId);
    }

    [RelayCommand]
    private async Task AddEquipmentAsync()
    {
        var newEquipment = new ProductionEquipmentDto();
        if (!_dialogService.ShowProductionEquipmentDialog(newEquipment, WorkshopOptions, false))
            return;

        await RunBusyAsync(async () =>
        {
            ValidateEquipment(newEquipment);
            var equipment = new ProductionEquipment
            {
                Name = newEquipment.Name.Trim(),
                SerialNumber = NormalizeOptionalText(newEquipment.SerialNumber),
                OkofCode = newEquipment.OkofCode.Trim(),
                InventoryNumber = newEquipment.InventoryNumber.Trim(),
                WorkshopId = newEquipment.WorkshopId
            };

            _dbContext.ProductionEquipments.Add(equipment);
            await _dbContext.SaveChangesAsync();
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
            ValidateEquipment(editEquipment);
            var equipment = await _dbContext.ProductionEquipments
                .FirstOrDefaultAsync(item => item.ProductionEquipmentId == editEquipment.Id);

            if (equipment == null)
                throw new InvalidOperationException("Оборудование не найдено.");

            equipment.Name = editEquipment.Name.Trim();
            equipment.SerialNumber = NormalizeOptionalText(editEquipment.SerialNumber);
            equipment.OkofCode = editEquipment.OkofCode.Trim();
            equipment.InventoryNumber = editEquipment.InventoryNumber.Trim();
            equipment.WorkshopId = editEquipment.WorkshopId;

            await _dbContext.SaveChangesAsync();
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
            var equipment = await _dbContext.ProductionEquipments
                .FirstOrDefaultAsync(item => item.ProductionEquipmentId == SelectedEquipment.Id);

            if (equipment == null)
                throw new InvalidOperationException("Оборудование не найдено.");

            _dbContext.ProductionEquipments.Remove(equipment);
            await _dbContext.SaveChangesAsync();
            SelectedEquipment = null;
            await LoadEquipmentAsync();
            StatusMessage = "Оборудование удалено.";
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

    private static void ValidateEquipment(ProductionEquipmentDto equipment)
    {
        if (string.IsNullOrWhiteSpace(equipment.Name))
            throw new InvalidOperationException("Укажите название оборудования.");

        if (string.IsNullOrWhiteSpace(equipment.OkofCode))
            throw new InvalidOperationException("Укажите код ОКОФ.");

        if (string.IsNullOrWhiteSpace(equipment.InventoryNumber))
            throw new InvalidOperationException("Укажите инвентарный номер.");

        if (equipment.WorkshopId <= 0)
            throw new InvalidOperationException("Выберите цех.");

        if (equipment.Name.Trim().Length > 255)
            throw new InvalidOperationException("Название оборудования не должно быть длиннее 255 символов.");

        if (NormalizeOptionalText(equipment.SerialNumber)?.Length > 20)
            throw new InvalidOperationException("Серийный номер не должен быть длиннее 20 символов.");

        if (equipment.OkofCode.Trim().Length > 19)
            throw new InvalidOperationException("Код ОКОФ не должен быть длиннее 19 символов.");

        if (equipment.InventoryNumber.Trim().Length > 17)
            throw new InvalidOperationException("Инвентарный номер не должен быть длиннее 17 символов.");
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string GetErrorMessage(Exception exception)
    {
        var current = exception;
        while (current.InnerException != null)
            current = current.InnerException;

        return current.Message;
    }
}
