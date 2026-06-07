using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using QualityControlSystem.Infrastructure;
using QualityControlSystem.Infrastructure.Entities;
using QualityControlSystem.WPF.Models;
using QualityControlSystem.WPF.Services.Interfaces;
using QualityControlSystem.WPF.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace QualityControlSystem.WPF.ViewModels
{
    public partial class OperatorControlViewModel : BaseViewModel
    {
        private readonly AppDbContext _dbContext;
        private readonly IEdgeDeviceService _edgeDeviceService;
        private readonly IDialogService _dialogService;
        private readonly INotificationService _notificationService;
        private readonly HttpClient _videoClient = new() { Timeout = TimeSpan.FromSeconds(3) };
        private readonly DispatcherTimer _frameTimer;
        private readonly DispatcherTimer _resultTimer;

        [ObservableProperty]
        private string _statusMessage = "Модуль контроля качества готов";

        [ObservableProperty]
        private string _currentMode = "Ожидание";

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private bool _isPhotomakerRunning;

        [ObservableProperty]
        private bool _isOperatingRunning;

        [ObservableProperty]
        private ImageSource? _videoFrame;

        [ObservableProperty]
        private string _lastLog = string.Empty;

        [ObservableProperty]
        private ObservableCollection<LookupItemDto> _frameOptions = new();

        [ObservableProperty]
        private ObservableCollection<LookupItemDto> _workshopOptions = new();

        [ObservableProperty]
        private ObservableCollection<ProductionEquipmentDto> _equipment = new();

        [ObservableProperty]
        private ProductionEquipmentDto? _selectedEquipment;

        [ObservableProperty]
        private LookupItemDto? _selectedFrame;

        [ObservableProperty]
        private ObservableCollection<string> _templateSides = new(["front", "left", "right", "top", "back"]);

        [ObservableProperty]
        private string _selectedTemplateSide = "front";

        [ObservableProperty]
        private int _inspectedFramesCount;

        [ObservableProperty]
        private int _passedFramesCount;

        public ObservableCollection<EdgeInspectionResultDto> Results { get; } = new();

        public OperatorControlViewModel(
            AppDbContext dbContext,
            IEdgeDeviceService edgeDeviceService,
            IDialogService dialogService,
            INotificationService notificationService)
        {
            _dbContext = dbContext;
            _edgeDeviceService = edgeDeviceService;
            _dialogService = dialogService;
            _notificationService = notificationService;

            _frameTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
            _frameTimer.Tick += async (_, _) => await LoadFrameAsync();

            _resultTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            _resultTimer.Tick += async (_, _) => await RefreshResultsSafeAsync();

            _ = LoadFramesAsync();
        }

        private async Task LoadFramesAsync()
        {
            try
            {
                var frames = await _edgeDeviceService.GetFrameOptionsAsync();
                FrameOptions.Clear();
                foreach (var frame in frames)
                    FrameOptions.Add(frame);

                SelectedFrame ??= FrameOptions.FirstOrDefault();
            }
            catch
            {
                // The screen can still be used for SSH/script diagnostics.
            }
        }

        private async Task LoadEquipmentAsync()
        {
            try
            {
                await LoadWorkshopOptionsAsync();
                await LoadProductionEquipmentAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка загрузки оборудования: {GetErrorMessage(ex)}";
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

        private async Task LoadProductionEquipmentAsync()
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

            await RunUiTaskAsync(async () =>
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
                await LoadProductionEquipmentAsync();
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

            await RunUiTaskAsync(async () =>
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
                await LoadProductionEquipmentAsync();
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

            await RunUiTaskAsync(async () =>
            {
                var equipment = await _dbContext.ProductionEquipments
                    .FirstOrDefaultAsync(item => item.ProductionEquipmentId == SelectedEquipment.Id);

                if (equipment == null)
                    throw new InvalidOperationException("Оборудование не найдено.");

                _dbContext.ProductionEquipments.Remove(equipment);
                await _dbContext.SaveChangesAsync();
                SelectedEquipment = null;
                await LoadProductionEquipmentAsync();
                StatusMessage = "Оборудование удалено.";
            });
        }

        [RelayCommand]
        private async Task CheckConnectionAsync()
        {
            await RunUiTaskAsync(async () =>
            {
                LastLog = await _edgeDeviceService.CheckConnectionAsync();
                StatusMessage = "Raspberry Pi доступна по SSH";
            });
        }

        [RelayCommand]
        private async Task DeployScriptsAsync()
        {
            await RunUiTaskAsync(async () =>
            {
                LastLog = await _edgeDeviceService.DeployScriptsAsync();
                StatusMessage = "Wrapper-скрипты загружены на Raspberry Pi";
            });
        }

        [RelayCommand]
        private async Task StartPhotomakerAsync()
        {
            await RunUiTaskAsync(async () =>
            {
                LastLog = await _edgeDeviceService.StartPhotomakerAsync();
                IsPhotomakerRunning = true;
                IsOperatingRunning = false;
                CurrentMode = "Создание шаблонов";
                StartVideo();
                StatusMessage = "Photomaker запущен. Кадр появится после прогрева камеры.";
            });
        }

        [RelayCommand]
        private async Task StopPhotomakerAsync()
        {
            await RunUiTaskAsync(async () =>
            {
                await _edgeDeviceService.StopPhotomakerAsync();
                IsPhotomakerRunning = false;
                StopVideoIfIdle();
                StatusMessage = "Photomaker остановлен";
            });
        }

        [RelayCommand]
        private async Task CaptureTemplateAsync()
        {
            await RunUiTaskAsync(async () =>
            {
                if (SelectedFrame?.Id is not int frameId)
                    throw new InvalidOperationException("Выберите модель каркаса для шаблона.");

                var templateId = await _edgeDeviceService.CaptureTemplateForFrameAsync(frameId, SelectedTemplateSide);
                LastLog = $"Создан шаблон #{templateId}: каркас {SelectedFrame.Name}, сторона {SelectedTemplateSide}";
                StatusMessage = "Шаблон сохранен на ПК и добавлен в таблицу template";
            });
        }

        [RelayCommand]
        private async Task SyncTemplatesAsync()
        {
            await RunUiTaskAsync(async () =>
            {
                LastLog = await _edgeDeviceService.SyncTemplatesAsync();
                StatusMessage = "Шаблоны синхронизированы на основной ПК";
            });
        }

        [RelayCommand]
        private async Task StartOperatingAsync()
        {
            await RunUiTaskAsync(async () =>
            {
                if (SelectedFrame?.Id is not int frameId)
                    throw new InvalidOperationException("Выберите модель каркаса для запуска operating.");

                LastLog = await _edgeDeviceService.StartOperatingForFrameAsync(frameId);
                IsOperatingRunning = true;
                IsPhotomakerRunning = false;
                CurrentMode = "Контроль деталей";
                StartVideo();
                _resultTimer.Start();
                StatusMessage = "Operating запущен. Результаты будут отображаться в таблице и попадут в отчет.";
            });
        }

        [RelayCommand]
        private async Task StopOperatingAsync()
        {
            await RunUiTaskAsync(async () =>
            {
                _resultTimer.Stop();
                await RefreshResultsAsync();
                await _edgeDeviceService.StopOperatingAsync();
                IsOperatingRunning = false;
                StopVideoIfIdle();
                StatusMessage = "Operating остановлен";
            });
        }

        [RelayCommand]
        private async Task CreateReportAsync()
        {
            await RunUiTaskAsync(async () =>
            {
                if (SelectedFrame?.Id is not int frameId)
                    throw new InvalidOperationException("Выберите модель каркаса для отчета.");

                if (IsOperatingRunning)
                    await RefreshResultsAsync();

                if (Results.Count == 0)
                    throw new InvalidOperationException("Нет результатов контроля для отчета.");

                var reportsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
                Directory.CreateDirectory(reportsDirectory);
                var safeFrameName = string.Join("_", SelectedFrame.Name.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
                var dialog = new SaveFileDialog
                {
                    Title = "Составить отчет",
                    Filter = "Текстовый отчет (*.txt)|*.txt|Все файлы (*.*)|*.*",
                    DefaultExt = ".txt",
                    AddExtension = true,
                    InitialDirectory = reportsDirectory,
                    FileName = $"quality_report_{safeFrameName}_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
                };

                if (dialog.ShowDialog() != true)
                    return;

                var path = await _edgeDeviceService.CreateQualityReportAsync(frameId, Results, dialog.FileName);
                LastLog = $"Отчет составлен: {path}";
                StatusMessage = "Отчет составлен";
            });
        }

        private async Task RefreshResultsAsync()
        {
            var results = await _edgeDeviceService.GetInspectionResultsAsync();
            var ordered = NumberResults(results);
            ApplyWeightCheck(ordered, SelectedFrame?.Weight);

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Results.Clear();
                foreach (var result in ordered)
                    Results.Add(result);

                InspectedFramesCount = ordered.Count;
                PassedFramesCount = ordered.Count(IsPassedResult);
            });
        }

        private async Task RefreshResultsSafeAsync()
        {
            try
            {
                await RefreshResultsAsync();
            }
            catch
            {
                // The Raspberry Pi process can restart between polling ticks.
            }
        }

        private async Task LoadFrameAsync()
        {
            if (!IsPhotomakerRunning && !IsOperatingRunning)
                return;

            try
            {
                var url = IsPhotomakerRunning
                    ? _edgeDeviceService.GetPhotomakerFrameUrl()
                    : _edgeDeviceService.GetOperatingFrameUrl();

                var bytes = await _videoClient.GetByteArrayAsync($"{url}?t={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");
                var image = new BitmapImage();
                using var stream = new MemoryStream(bytes);
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = stream;
                image.EndInit();
                image.Freeze();
                VideoFrame = image;
            }
            catch
            {
                // The camera can be warming up or the process can be restarting.
            }
        }

        private void StartVideo()
        {
            if (!_frameTimer.IsEnabled)
                _frameTimer.Start();
        }

        private void StopVideoIfIdle()
        {
            if (!IsPhotomakerRunning && !IsOperatingRunning)
            {
                _frameTimer.Stop();
                VideoFrame = null;
                CurrentMode = "Ожидание";
            }
        }

        private async Task RunUiTaskAsync(Func<Task> action)
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

        private static List<EdgeInspectionResultDto> NumberResults(IEnumerable<EdgeInspectionResultDto> results)
        {
            var chronological = results
                .Where(result => result.RecordedAt != default)
                .GroupBy(result => new { result.RecordedAt, result.Id })
                .Select(group => group.First())
                .OrderBy(result => result.RecordedAt)
                .ThenBy(result => result.Id)
                .ToList();

            for (var index = 0; index < chronological.Count; index++)
                chronological[index].ControlNumber = index + 1;

            return chronological
                .OrderByDescending(result => result.RecordedAt)
                .ThenByDescending(result => result.Id)
                .ToList();
        }

        private static bool IsPassedStatus(string? status)
        {
            return string.Equals(status, "OK", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "Годен", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "Passed", StringComparison.OrdinalIgnoreCase);
        }

        private static void ApplyWeightCheck(IEnumerable<EdgeInspectionResultDto> results, double? expectedWeight)
        {
            foreach (var result in results)
            {
                result.ExpectedWeight = expectedWeight;
                result.WeightTolerance = expectedWeight.HasValue
                    ? EdgeInspectionResultDto.GetDefaultWeightTolerance(expectedWeight.Value)
                    : null;
            }
        }

        private static bool IsPassedResult(EdgeInspectionResultDto result)
        {
            if (!IsPassedStatusForQualityResult(result.Status))
                return false;

            if (!result.ExpectedWeight.HasValue || !result.Weight.HasValue)
                return false;

            var tolerance = result.WeightTolerance ?? EdgeInspectionResultDto.GetDefaultWeightTolerance(result.ExpectedWeight.Value);
            return Math.Abs(result.Weight.Value - result.ExpectedWeight.Value) <= tolerance;
        }

        private static bool IsPassedStatusForQualityResult(string? status)
        {
            return string.Equals(status, "OK", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "Годен", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "Passed", StringComparison.OrdinalIgnoreCase);
        }
    }
}
