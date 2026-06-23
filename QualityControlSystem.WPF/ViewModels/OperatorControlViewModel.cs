using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using QualityControlSystem.WPF.Constants;
using QualityControlSystem.WPF.Dtos;
using QualityControlSystem.WPF.Services.Interfaces;
using QualityControlSystem.WPF.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using TemplateSideValues = QualityControlSystem.WPF.Constants.TemplateSides;

namespace QualityControlSystem.WPF.ViewModels
{
    public partial class OperatorControlViewModel : BaseViewModel, IAsyncInitializable, IDisposable
    {
        private readonly IOperatorControlDataService _operatorControlDataService;
        private readonly IOperatorInspectionSessionService _operatorInspectionSessionService;
        private readonly IOperatorInspectionResultService _operatorInspectionResultService;
        private readonly IOperatorReportService _operatorReportService;
        private readonly IOperatorVideoFrameService _operatorVideoFrameService;
        private readonly IEquipmentManagementService _equipmentManagementService;
        private readonly IEquipmentWorkResultsService _equipmentWorkResultsService;
        private readonly IDialogService _dialogService;
        private readonly INotificationService _notificationService;
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
            IOperatorInspectionSessionService operatorInspectionSessionService,
            IOperatorInspectionResultService operatorInspectionResultService,
            IOperatorReportService operatorReportService,
            IOperatorVideoFrameService operatorVideoFrameService,
            IEquipmentManagementService equipmentManagementService,
            IEquipmentWorkResultsService equipmentWorkResultsService,
            IDialogService dialogService,
            INotificationService notificationService)
        {
            _operatorControlDataService = operatorControlDataService;
            _operatorInspectionSessionService = operatorInspectionSessionService;
            _operatorInspectionResultService = operatorInspectionResultService;
            _operatorReportService = operatorReportService;
            _operatorVideoFrameService = operatorVideoFrameService;
            _equipmentManagementService = equipmentManagementService;
            _equipmentWorkResultsService = equipmentWorkResultsService;
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
                var frames = await _operatorInspectionSessionService.GetFrameOptionsAsync();
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
                LastLog = await _operatorInspectionSessionService.CheckConnectionAsync();
                StatusMessage = "Raspberry Pi доступна по SSH";
            });
        }

        [RelayCommand]
        private async Task DeployScriptsAsync()
        {
            await RunUiTaskAsync(async () =>
            {
                LastLog = await _operatorInspectionSessionService.DeployScriptsAsync();
                StatusMessage = "Wrapper-скрипты загружены на Raspberry Pi";
            });
        }

        [RelayCommand]
        private async Task StartPhotomakerAsync()
        {
            await RunUiTaskAsync(async () =>
            {
                LastLog = await _operatorInspectionSessionService.StartPhotomakerAsync();
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
                await _operatorInspectionSessionService.StopPhotomakerAsync();
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

                var templateId = await _operatorInspectionSessionService.CaptureTemplateForFrameAsync(frameId, SelectedTemplateSide);
                LastLog = $"Создан шаблон #{templateId}: каркас {SelectedFrame.Name}, сторона {SelectedTemplateSide}";
                StatusMessage = "Шаблон сохранен на ПК и добавлен в таблицу template";
            });
        }

        [RelayCommand]
        private async Task SyncTemplatesAsync()
        {
            await RunUiTaskAsync(async () =>
            {
                LastLog = await _operatorInspectionSessionService.SyncTemplatesAsync();
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

                LastLog = await _operatorInspectionSessionService.StartOperatingForFramesAsync(selectedFrames.Select(frame => frame.Id!.Value));
                IsOperatingRunning = true;
                IsPhotomakerRunning = false;
                CurrentMode = "Модуль контроля качества";
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

                await _operatorInspectionSessionService.StopOperatingAsync();
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
                if (IsOperatingRunning)
                    await RefreshResultsAsync();

                var selectedFrames = GetSelectedInspectionFrames();
                var selectedFrameIds = selectedFrames
                    .Where(frame => frame.Id.HasValue)
                    .Select(frame => frame.Id!.Value)
                    .ToList();
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

                var path = await _operatorReportService.CreateQualityReportAsync(new OperatorReportRequest
                {
                    FrameIds = selectedFrameIds,
                    Results = Results.ToList(),
                    OutputPath = dialog.FileName
                });
                LastLog = $"Отчет составлен: {path}";
                StatusMessage = "Отчет составлен";
            });
        }

        private async Task RefreshResultsAsync()
        {
            var results = await _operatorInspectionSessionService.GetInspectionResultsAsync();
            var selectedFrames = GetSelectedInspectionFrames();
            var selectedFrameIds = selectedFrames
                .Where(frame => frame.Id.HasValue)
                .Select(frame => frame.Id!.Value)
                .Distinct()
                .ToList();

            var templateFrameMap = await _operatorControlDataService.GetTemplateFrameMapAsync(selectedFrameIds);
            var fallbackFrame = BuildFallbackFrameInfo(selectedFrames);
            var items = _operatorInspectionResultService.BuildItems(results, templateFrameMap, fallbackFrame);
            var summary = _operatorInspectionResultService.BuildSummary(items);

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Results.Clear();
                foreach (var result in items)
                    Results.Add(result);

                InspectedFramesCount = summary.TotalCount;
                PassedFramesCount = summary.PassedCount;
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
                VideoFrame = IsPhotomakerRunning
                    ? await _operatorVideoFrameService.GetPhotomakerFrameAsync()
                    : await _operatorVideoFrameService.GetOperatingFrameAsync();
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
                        await _operatorInspectionSessionService.StopPhotomakerAsync();

                    if (stopOperating)
                        await _operatorInspectionSessionService.StopOperatingAsync();
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

        private static OperatorFrameInspectionInfoDto? BuildFallbackFrameInfo(IReadOnlyList<SelectableFrameDto> selectedFrames)
        {
            if (selectedFrames.Count != 1 || selectedFrames[0].Id is not int frameId)
                return null;

            return new OperatorFrameInspectionInfoDto
            {
                FrameId = frameId,
                FrameName = selectedFrames[0].Name,
                ExpectedWeight = selectedFrames[0].Weight
            };
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
