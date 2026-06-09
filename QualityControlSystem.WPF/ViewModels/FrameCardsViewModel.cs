using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QualityControlSystem.WPF.Constants;
using QualityControlSystem.WPF.Dtos;
using QualityControlSystem.WPF.Services.Interfaces;
using QualityControlSystem.WPF.ViewModels.Base;

namespace QualityControlSystem.WPF.ViewModels;

public partial class FrameCardsViewModel : BaseViewModel, IAsyncInitializable
{
    private const string AllMaterialsFilter = "Все материалы";

    private readonly IFrameCardService _frameCardService;
    private readonly IDialogService _dialogService;
    private bool _isInitialized;

    [ObservableProperty]
    private ObservableCollection<FrameCardDto> _frames = new();

    [ObservableProperty]
    private ObservableCollection<LookupItemDto> _materialOptions = new();

    [ObservableProperty]
    private ObservableCollection<LookupItemDto> _workshopOptions = new();

    [ObservableProperty]
    private System.ComponentModel.ICollectionView? _framesView;

    [ObservableProperty]
    private FrameCardDto? _selectedFrame;

    [ObservableProperty]
    private LookupItemDto? _selectedMaterialFilter;

    [ObservableProperty]
    private LookupItemDto? _selectedWorkshopFilter;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    public FrameCardsViewModel(
        IFrameCardService frameCardService,
        IDialogService dialogService)
    {
        _frameCardService = frameCardService;
        _dialogService = dialogService;
        FramesView = CollectionViewSource.GetDefaultView(Frames);
        FramesView.Filter = FilterFrame;
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
        StatusMessage = "Загрузка карточек каркасов...";

        try
        {
            await LoadLookupsAsync();
            await LoadFramesAsync();
            StatusMessage = Frames.Count == 0
                ? "Карточки каркасов не найдены."
                : $"Карточек каркасов: {Frames.Count}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка загрузки: {GetErrorMessage(ex)}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadLookupsAsync()
    {
        var materialFilter = SelectedMaterialFilter?.Id;
        var workshopFilter = SelectedWorkshopFilter?.Id;

        var materials = await _frameCardService.GetMaterialTypesAsync();
        var workshops = await _frameCardService.GetWorkshopsAsync();

        MaterialOptions.Clear();
        MaterialOptions.Add(new LookupItemDto { Id = null, Name = AllMaterialsFilter });
        foreach (var material in materials)
            MaterialOptions.Add(material);

        WorkshopOptions.Clear();
        if (_frameCardService.CurrentWorkshopId is null)
            WorkshopOptions.Add(new LookupItemDto { Id = null, Name = UiFilterOptions.AllWorkshops });
        foreach (var workshop in workshops)
            WorkshopOptions.Add(workshop);

        SelectedMaterialFilter = MaterialOptions.FirstOrDefault(item => item.Id == materialFilter) ?? MaterialOptions.FirstOrDefault();
        SelectedWorkshopFilter = _frameCardService.CurrentWorkshopId is int userWorkshopId
            ? WorkshopOptions.FirstOrDefault(item => item.Id == userWorkshopId)
            : WorkshopOptions.FirstOrDefault(item => item.Id == workshopFilter) ?? WorkshopOptions.FirstOrDefault();
    }

    private async Task LoadFramesAsync()
    {
        var selectedId = SelectedFrame?.Id;
        var frames = await _frameCardService.GetFramesAsync();

        Frames.Clear();
        foreach (var frame in frames)
            Frames.Add(frame);

        SelectedFrame = Frames.FirstOrDefault(frame => frame.Id == selectedId);
        FramesView?.Refresh();
    }

    [RelayCommand]
    private async Task AddFrameAsync()
    {
        var newFrame = new FrameCardDto
        {
            WorkshopId = _frameCardService.CurrentWorkshopId.GetValueOrDefault()
        };
        if (!_dialogService.ShowFrameDialog(newFrame, MaterialOptions.Where(item => item.Id.HasValue), WorkshopOptions.Where(item => item.Id.HasValue), false))
            return;

        IsBusy = true;
        StatusMessage = "Добавление карточки каркаса...";

        try
        {
            await _frameCardService.AddFrameAsync(newFrame);
            await LoadFramesAsync();
            StatusMessage = "Карточка каркаса добавлена.";
        }
        catch (Exception ex)
        {
            var message = $"Не удалось добавить карточку: {GetErrorMessage(ex)}";
            StatusMessage = message;
            _dialogService.ShowMessage(message, "Ошибка");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task EditFrameAsync()
    {
        if (SelectedFrame == null)
        {
            StatusMessage = "Выберите каркас.";
            return;
        }

        var editFrame = new FrameCardDto
        {
            Id = SelectedFrame.Id,
            Name = SelectedFrame.Name,
            MaterialTypeId = SelectedFrame.MaterialTypeId,
            WorkshopId = SelectedFrame.WorkshopId,
            Weight = SelectedFrame.Weight,
            Length = SelectedFrame.Length,
            Width = SelectedFrame.Width,
            Height = SelectedFrame.Height,
            ImagePath = SelectedFrame.ImagePath
        };

        if (!_dialogService.ShowFrameDialog(editFrame, MaterialOptions.Where(item => item.Id.HasValue), WorkshopOptions.Where(item => item.Id.HasValue), true))
            return;

        IsBusy = true;
        StatusMessage = "Обновление карточки каркаса...";

        try
        {
            await _frameCardService.UpdateFrameAsync(editFrame);
            await LoadFramesAsync();
            StatusMessage = "Карточка каркаса обновлена.";
        }
        catch (Exception ex)
        {
            var message = $"Не удалось обновить карточку: {GetErrorMessage(ex)}";
            StatusMessage = message;
            _dialogService.ShowMessage(message, "Ошибка");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteFrameAsync()
    {
        if (SelectedFrame == null)
        {
            StatusMessage = "Выберите каркас.";
            return;
        }

        if (!_dialogService.ShowConfirm($"Удалить каркас \"{SelectedFrame.Name}\"?"))
            return;

        IsBusy = true;
        StatusMessage = "Удаление карточки каркаса...";

        try
        {
            await _frameCardService.DeleteFrameAsync(SelectedFrame.Id);
            SelectedFrame = null;
            await LoadFramesAsync();
            StatusMessage = "Карточка каркаса удалена.";
        }
        catch (Exception ex)
        {
            var message = $"Не удалось удалить карточку: {GetErrorMessage(ex)}";
            StatusMessage = message;
            _dialogService.ShowMessage(message, "Ошибка");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool FilterFrame(object item)
    {
        if (item is not FrameCardDto frame)
            return false;

        if (SelectedMaterialFilter?.Id.HasValue == true && frame.MaterialTypeId != SelectedMaterialFilter.Id.Value)
            return false;

        if (SelectedWorkshopFilter?.Id.HasValue == true && frame.WorkshopId != SelectedWorkshopFilter.Id.Value)
            return false;

        if (string.IsNullOrWhiteSpace(SearchText))
            return true;

        var search = SearchText.Trim();
        return Contains(frame.Name, search)
            || Contains(frame.MaterialName, search)
            || Contains(frame.WorkshopName, search)
            || Contains(frame.ImagePath, search)
            || frame.Id.ToString().Contains(search, StringComparison.OrdinalIgnoreCase);
    }

    private static bool Contains(string? value, string search)
    {
        return value?.Contains(search, StringComparison.OrdinalIgnoreCase) == true;
    }

    private static string GetErrorMessage(Exception exception)
    {
        var current = exception;
        while (current.InnerException != null)
            current = current.InnerException;

        return current.Message;
    }

    partial void OnSearchTextChanged(string value) => FramesView?.Refresh();

    partial void OnSelectedMaterialFilterChanged(LookupItemDto? value) => FramesView?.Refresh();

    partial void OnSelectedWorkshopFilterChanged(LookupItemDto? value) => FramesView?.Refresh();
}
