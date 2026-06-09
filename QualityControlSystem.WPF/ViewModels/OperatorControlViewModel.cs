using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using QualityControlSystem.WPF.Constants;
using QualityControlSystem.WPF.Dtos;
using QualityControlSystem.WPF.Services.Interfaces;
using QualityControlSystem.WPF.Validation;
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
using TemplateSideValues = QualityControlSystem.WPF.Constants.TemplateSides;

namespace QualityControlSystem.WPF.ViewModels
{
    public partial class OperatorControlViewModel : BaseViewModel, IAsyncInitializable, IDisposable
    {
        private readonly IOperatorControlDataService _operatorControlDataService;
        private readonly IEdgeDeviceService _edgeDeviceService;
        private readonly IEquipmentManagementService _equipmentManagementService;
        private readonly IEquipmentWorkResultsService _equipmentWorkResultsService;
        private readonly IEquipmentValidator _equipmentValidator;
        private readonly IDialogService _dialogService;
        private readonly INotificationService _notificationService;
        private readonly HttpClient _videoClient = new() { Timeout = TimeSpan.FromSeconds(3) };
        private readonly DispatcherTimer _frameTimer;
        private readonly DispatcherTimer _resultTimer;
        private bool _isInitialized;
        private bool _isDisposed;

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
        private ObservableCollection<SelectableFrameDto> _inspectionFrameOptions = new();

        [ObservableProperty]
        private ObservableCollection<LookupItemDto> _workshopOptions = new();

        [ObservableProperty]
        private ObservableCollection<ProductionEquipmentDto> _equipment = new();

        [ObservableProperty]
        private ProductionEquipmentDto? _selectedEquipment;

        [ObservableProperty]
        private LookupItemDto? _selectedFrame;

        [ObservableProperty]
        private ObservableCollection<string> _templateSides = new(TemplateSideValues.All);

        [ObservableProperty]
        private string _selectedTemplateSide = TemplateSideValues.Front;

        [ObservableProperty]
        private int _inspectedFramesCount;

        [ObservableProperty]
        private int _passedFramesCount;

        public ObservableCollection<EdgeInspectionResultDto> Results { get; } = new();

        public OperatorControlViewModel(
            IOperatorControlDataService operatorControlDataService,
            IEdgeDeviceService edgeDeviceService,
            IEquipmentManagementService equipmentManagementService,
            IEquipmentWorkResultsService equipmentWorkResultsService,
            IEquipmentValidator equipmentValidator,
            IDialogService dialogService,
            INotificationService notificationService)
        {
            _operatorControlDataService = operatorControlDataService;
            _edgeDeviceService = edgeDeviceService;
            _equipmentManagementService = equipmentManagementService;
            _equipmentWorkResultsService = equipmentWorkResultsService;
            _equipmentValidator = equipmentValidator;
            _dialogService = dialogService;
            _notificationService = notificationService;

            _frameTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
            _frameTimer.Tick += async (_, _) => await LoadFrameAsync();

            _resultTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            _resultTimer.Tick += async (_, _) => await RefreshResultsSafeAsync();
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized)
                return;

            _isInitialized = true;
            await LoadFramesAsync();
        }

        private async Task LoadFramesAsync()
        {
            try
            {
                var frames = await _edgeDeviceService.GetFrameOptionsAsync();
                FrameOptions.Clear();
                InspectionFrameOptions.Clear();
                foreach (var frame in frames)
                {
                    FrameOptions.Add(frame);
                    InspectionFrameOptions.Add(new SelectableFrameDto
                    {
                        Id = frame.Id,
                        Name = frame.Name,
                        Weight = frame.Weight
                    });
                }

                SelectedFrame ??= FrameOptions.FirstOrDefault();
                if (!InspectionFrameOptions.Any(frame => frame.IsSelected)
                    && InspectionFrameOptions.FirstOrDefault(frame => frame.Id == SelectedFrame?.Id) is { } selectedFrame)
                {
                    selectedFrame.IsSelected = true;
                }
            }
            catch
            {
                // The screen can still be used for SSH/script diagnostics.
            }
        }

        private async Task LoadWorkshopOptionsAsync()
        {
            var workshops = await _equipmentManagementService.GetWorkshopOptionsAsync();

            WorkshopOptions.Clear();
            foreach (var workshop in workshops)
                WorkshopOptions.Add(workshop);
        }

        private async Task LoadProductionEquipmentAsync()
        {
            var selectedId = SelectedEquipment?.Id;
            var rows = await _equipmentManagementService.GetEquipmentAsync();

            Equipment.Clear();
            foreach (var item in rows)
                Equipment.Add(item);

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
                EnsureEquipmentIsValid(newEquipment);
                await _equipmentManagementService.AddEquipmentAsync(newEquipment);
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
                EnsureEquipmentIsValid(editEquipment);
                await _equipmentManagementService.UpdateEquipmentAsync(editEquipment);
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
                await _equipmentManagementService.DeleteEquipmentAsync(SelectedEquipment.Id);
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
                var selectedFrames = GetSelectedInspectionFrames();
                if (selectedFrames.Count == 0)
                    throw new InvalidOperationException("Выберите модель каркаса для шаблона.");

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
                var selectedFrames = GetSelectedInspectionFrames();
                if (selectedFrames.Count == 0)
                    throw new InvalidOperationException("Выберите модель каркаса для запуска operating.");

                LastLog = await _edgeDeviceService.StartOperatingForFramesAsync(selectedFrames.Select(frame => frame.Id!.Value));
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
                foreach (var frame in GetSelectedInspectionFrames())
                {
                    var frameResults = Results.Where(result => result.FrameId == frame.Id).ToList();
                    if (frameResults.Count > 0 && frame.Id.HasValue)
                        await _equipmentWorkResultsService.CreateResultsForFrameInspectionAsync(frame.Id.Value, frameResults);
                }

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

                var selectedFrames = GetSelectedInspectionFrames();
                var reportsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
                Directory.CreateDirectory(reportsDirectory);
                var reportName = selectedFrames.Count == 1 ? selectedFrames[0].Name : "multi_frames";
                var safeFrameName = string.Join("_", reportName.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
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

                var path = await _edgeDeviceService.CreateQualityReportAsync(selectedFrames.Select(frame => frame.Id!.Value), Results, dialog.FileName);
                LastLog = $"Отчет составлен: {path}";
                StatusMessage = "Отчет составлен";
            });
        }

        private async Task RefreshResultsAsync()
        {
            var results = await _edgeDeviceService.GetInspectionResultsAsync();
            var ordered = NumberResults(results);
            await ApplyFrameChecksAsync(ordered);

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

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            _frameTimer.Stop();
            _resultTimer.Stop();
            _videoClient.Dispose();

            var stopPhotomaker = IsPhotomakerRunning;
            var stopOperating = IsOperatingRunning;
            IsPhotomakerRunning = false;
            IsOperatingRunning = false;

            if (!stopPhotomaker && !stopOperating)
                return;

            _ = Task.Run(async () =>
            {
                try
                {
                    if (stopPhotomaker)
                        await _edgeDeviceService.StopPhotomakerAsync();

                    if (stopOperating)
                        await _edgeDeviceService.StopOperatingAsync();
                }
                catch
                {
                    // The view is already closed; stop failures are surfaced on the next SSH check/start.
                }
            });
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

        private void EnsureEquipmentIsValid(ProductionEquipmentDto equipment)
        {
        var result = _equipmentValidator.Validate(equipment);
        if (!result.IsValid)
            throw new InvalidOperationException(result.ErrorMessage ?? "Данные оборудования заполнены некорректно.");
    }

        private List<SelectableFrameDto> GetSelectedInspectionFrames()
        {
            var selectedFrames = InspectionFrameOptions
                .Where(frame => frame.IsSelected && frame.Id.HasValue)
                .ToList();

            if (selectedFrames.Count == 0 && SelectedFrame?.Id.HasValue == true)
            {
                selectedFrames.Add(new SelectableFrameDto
                {
                    Id = SelectedFrame.Id,
                    Name = SelectedFrame.Name,
                    Weight = SelectedFrame.Weight,
                    IsSelected = true
                });
            }

            return selectedFrames;
        }

        private async Task ApplyFrameChecksAsync(List<EdgeInspectionResultDto> results)
        {
            var selectedFrames = GetSelectedInspectionFrames();
            var selectedFrameIds = selectedFrames
                .Where(frame => frame.Id.HasValue)
                .Select(frame => frame.Id!.Value)
                .Distinct()
                .ToList();

            var templateFrameMap = await _operatorControlDataService.GetTemplateFrameMapAsync(selectedFrameIds);
            var fallbackFrame = selectedFrames.Count == 1 ? selectedFrames[0] : null;

            foreach (var result in results)
            {
                if (templateFrameMap.TryGetValue(result.TemplateId, out var frameInfo))
                {
                    result.FrameId = frameInfo.FrameId;
                    result.FrameName = frameInfo.FrameName;
                    result.ExpectedWeight = frameInfo.ExpectedWeight;
                }
                else if (fallbackFrame?.Id.HasValue == true)
                {
                    result.FrameId = fallbackFrame.Id;
                    result.FrameName = fallbackFrame.Name;
                    result.ExpectedWeight = fallbackFrame.Weight;
                }

                result.WeightTolerance = result.ExpectedWeight.HasValue
                    ? EdgeInspectionResultDto.GetDefaultWeightTolerance(result.ExpectedWeight.Value)
                    : null;
            }
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
            return string.Equals(status, InspectionStatuses.Ok, StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, InspectionStatuses.AcceptedRu, StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, InspectionStatuses.Passed, StringComparison.OrdinalIgnoreCase);
        }
    }

    public partial class SelectableFrameDto : ObservableObject
    {
        public int? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public double? Weight { get; set; }

        [ObservableProperty]
        private bool _isSelected;
    }

}
