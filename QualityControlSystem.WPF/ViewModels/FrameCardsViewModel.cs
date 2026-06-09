using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using QualityControlSystem.Infrastructure;
using QualityControlSystem.Infrastructure.Entities;
using QualityControlSystem.WPF.Constants;
using QualityControlSystem.WPF.Dtos;
using QualityControlSystem.WPF.Services.Interfaces;
using QualityControlSystem.WPF.ViewModels.Base;

namespace QualityControlSystem.WPF.ViewModels;

public partial class FrameCardsViewModel : BaseViewModel
{
    private static readonly string[] SupportedImageExtensions = [".png", ".jpg", ".jpeg"];

    private readonly AppDbContext _dbContext;
    private readonly IDialogService _dialogService;
    private readonly IAuthService _authService;

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
        AppDbContext dbContext,
        IDialogService dialogService,
        IAuthService authService)
    {
        _dbContext = dbContext;
        _dialogService = dialogService;
        _authService = authService;
        FramesView = CollectionViewSource.GetDefaultView(Frames);
        FramesView.Filter = FilterFrame;
        _ = LoadAsync();
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

        var materials = await _dbContext.MaterialTypes
            .AsNoTracking()
            .OrderBy(material => material.Name)
            .Select(material => new LookupItemDto
            {
                Id = material.MaterialTypeId,
                Name = material.Name
            })
            .ToListAsync();

        var workshopQuery = _dbContext.Workshops.AsNoTracking();
        if (CurrentWorkshopId is int currentWorkshopId)
            workshopQuery = workshopQuery.Where(workshop => workshop.WorkshopId == currentWorkshopId);

        var workshops = await workshopQuery
            .OrderBy(workshop => workshop.Number)
            .Select(workshop => new LookupItemDto
            {
                Id = workshop.WorkshopId,
                Name = string.IsNullOrWhiteSpace(workshop.Purpose)
                    ? $"Цех {workshop.Number}"
                    : $"Цех {workshop.Number} - {workshop.Purpose}"
            })
            .ToListAsync();

        MaterialOptions.Clear();
        MaterialOptions.Add(new LookupItemDto { Id = null, Name = "Все материалы" });
        foreach (var material in materials)
            MaterialOptions.Add(material);

        WorkshopOptions.Clear();
        if (CurrentWorkshopId is null)
            WorkshopOptions.Add(new LookupItemDto { Id = null, Name = UiFilterOptions.AllWorkshops });
        foreach (var workshop in workshops)
            WorkshopOptions.Add(workshop);

        SelectedMaterialFilter = MaterialOptions.FirstOrDefault(item => item.Id == materialFilter) ?? MaterialOptions.FirstOrDefault();
        SelectedWorkshopFilter = CurrentWorkshopId is int userWorkshopId
            ? WorkshopOptions.FirstOrDefault(item => item.Id == userWorkshopId)
            : WorkshopOptions.FirstOrDefault(item => item.Id == workshopFilter) ?? WorkshopOptions.FirstOrDefault();
    }

    private async Task LoadFramesAsync()
    {
        var selectedId = SelectedFrame?.Id;
        IQueryable<Frame> frameQuery = _dbContext.Frames
            .AsNoTracking()
            .Include(frame => frame.MaterialType)
            .Include(frame => frame.Workshop);

        if (CurrentWorkshopId is int currentWorkshopId)
            frameQuery = frameQuery.Where(frame => frame.WorkshopId == currentWorkshopId);

        var frames = await frameQuery
            .OrderBy(frame => frame.FrameId)
            .Select(frame => new
            {
                frame.FrameId,
                frame.Name,
                frame.MaterialTypeId,
                MaterialName = frame.MaterialType.Name,
                frame.WorkshopId,
                frame.Workshop.Number,
                frame.Workshop.Purpose,
                frame.Weight,
                frame.Length,
                frame.Width,
                frame.Height,
                frame.ImagePath
            })
            .ToListAsync();

        Frames.Clear();
        foreach (var frame in frames)
        {
            Frames.Add(new FrameCardDto
            {
                Id = frame.FrameId,
                Name = frame.Name,
                MaterialTypeId = frame.MaterialTypeId,
                MaterialName = frame.MaterialName,
                WorkshopId = frame.WorkshopId,
                WorkshopNumber = frame.Number,
                WorkshopName = string.IsNullOrWhiteSpace(frame.Purpose)
                    ? $"Цех {frame.Number}"
                    : $"Цех {frame.Number} - {frame.Purpose}",
                Weight = frame.Weight,
                Length = frame.Length,
                Width = frame.Width,
                Height = frame.Height,
                ImagePath = frame.ImagePath,
                ImagePreview = LoadImagePreview(frame.ImagePath)
            });
        }

        SelectedFrame = Frames.FirstOrDefault(frame => frame.Id == selectedId);
        FramesView?.Refresh();
    }

    [RelayCommand]
    private async Task AddFrameAsync()
    {
        var newFrame = new FrameCardDto
        {
            WorkshopId = CurrentWorkshopId.GetValueOrDefault()
        };
        if (!_dialogService.ShowFrameDialog(newFrame, MaterialOptions.Where(item => item.Id.HasValue), WorkshopOptions.Where(item => item.Id.HasValue), false))
            return;

        IsBusy = true;
        StatusMessage = "Добавление карточки каркаса...";

        try
        {
            ApplyCurrentWorkshop(newFrame);
            ValidateFrame(newFrame);
            var frame = new Frame
            {
                Name = newFrame.Name.Trim(),
                MaterialTypeId = newFrame.MaterialTypeId,
                WorkshopId = newFrame.WorkshopId,
                Weight = newFrame.Weight,
                Length = newFrame.Length,
                Width = newFrame.Width,
                Height = newFrame.Height,
                ImagePath = NormalizeImagePath(newFrame.ImagePath)
            };

            _dbContext.Frames.Add(frame);
            await _dbContext.SaveChangesAsync();
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
            ApplyCurrentWorkshop(editFrame);
            ValidateFrame(editFrame);
            var frame = await _dbContext.Frames.FirstOrDefaultAsync(item => item.FrameId == editFrame.Id);
            if (frame == null)
                throw new InvalidOperationException("Каркас не найден.");

            frame.Name = editFrame.Name.Trim();
            frame.MaterialTypeId = editFrame.MaterialTypeId;
            frame.WorkshopId = editFrame.WorkshopId;
            frame.Weight = editFrame.Weight;
            frame.Length = editFrame.Length;
            frame.Width = editFrame.Width;
            frame.Height = editFrame.Height;
            frame.ImagePath = NormalizeImagePath(editFrame.ImagePath);

            await _dbContext.SaveChangesAsync();
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
            var frame = await _dbContext.Frames.FirstOrDefaultAsync(item => item.FrameId == SelectedFrame.Id);
            if (frame == null)
                throw new InvalidOperationException("Каркас не найден.");

            _dbContext.Frames.Remove(frame);
            await _dbContext.SaveChangesAsync();
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

    private static void ValidateFrame(FrameCardDto frame)
    {
        if (string.IsNullOrWhiteSpace(frame.Name))
            throw new InvalidOperationException("Укажите название каркаса.");

        if (frame.MaterialTypeId <= 0)
            throw new InvalidOperationException("Выберите материал.");

        if (frame.WorkshopId <= 0)
            throw new InvalidOperationException("Выберите цех.");

        var imagePath = NormalizeImagePath(frame.ImagePath);
        if (imagePath != null && !IsSupportedImagePath(imagePath))
            throw new InvalidOperationException("Путь к изображению должен указывать на PNG или JPEG файл.");
    }

    private static string? NormalizeImagePath(string? imagePath)
    {
        return string.IsNullOrWhiteSpace(imagePath) ? null : imagePath.Trim();
    }

    private static bool Contains(string? value, string search)
    {
        return value?.Contains(search, StringComparison.OrdinalIgnoreCase) == true;
    }

    private static bool IsSupportedImagePath(string path)
    {
        var extension = Path.GetExtension(path);
        return SupportedImageExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }

    private static ImageSource? LoadImagePreview(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath) || !IsSupportedImagePath(imagePath))
            return null;

        try
        {
            var absolutePath = Path.GetFullPath(imagePath);
            if (!File.Exists(absolutePath))
                return null;

            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.DecodePixelWidth = 96;
            image.UriSource = new Uri(absolutePath, UriKind.Absolute);
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch
        {
            return null;
        }
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

    private int? CurrentWorkshopId => _authService.CurrentUser?.WorkshopId;

    private void ApplyCurrentWorkshop(FrameCardDto frame)
    {
        if (CurrentWorkshopId is not int workshopId)
            return;

        if (frame.WorkshopId > 0 && frame.WorkshopId != workshopId)
            throw new InvalidOperationException("Нельзя добавлять или изменять каркасы другого цеха.");

        frame.WorkshopId = workshopId;
    }
}
