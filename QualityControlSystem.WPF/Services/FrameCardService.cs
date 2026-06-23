using Microsoft.EntityFrameworkCore;
using QualityControlSystem.Infrastructure;
using QualityControlSystem.Infrastructure.Entities;
using QualityControlSystem.WPF.Dtos;
using QualityControlSystem.WPF.Services.Interfaces;
using QualityControlSystem.WPF.Validation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace QualityControlSystem.WPF.Services;

public class FrameCardService : IFrameCardService
{
    private static readonly string[] SupportedImageExtensions = [".png", ".jpg", ".jpeg"];

    private readonly AppDbContext _dbContext;
    private readonly IAuthService _authService;
    private readonly IFrameCardValidator _frameCardValidator;

    public FrameCardService(
        AppDbContext dbContext,
        IAuthService authService,
        IFrameCardValidator frameCardValidator)
    {
        _dbContext = dbContext;
        _authService = authService;
        _frameCardValidator = frameCardValidator;
    }

    public async Task<IReadOnlyList<FrameCardDto>> GetFramesAsync(CancellationToken cancellationToken = default)
    {
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
            .ToListAsync(cancellationToken);

        return frames.Select(frame => new FrameCardDto
        {
            Id = frame.FrameId,
            Name = frame.Name,
            MaterialTypeId = frame.MaterialTypeId,
            MaterialName = frame.MaterialName,
            WorkshopId = frame.WorkshopId,
            WorkshopNumber = frame.Number,
            WorkshopName = FormatWorkshop(frame.Number, frame.Purpose),
            Weight = frame.Weight,
            Length = frame.Length,
            Width = frame.Width,
            Height = frame.Height,
            ImagePath = frame.ImagePath,
            ImagePreview = LoadImagePreview(frame.ImagePath)
        }).ToList();
    }

    public async Task<IReadOnlyList<LookupItemDto>> GetMaterialTypesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.MaterialTypes
            .AsNoTracking()
            .OrderBy(material => material.Name)
            .Select(material => new LookupItemDto
            {
                Id = material.MaterialTypeId,
                Name = material.Name
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LookupItemDto>> GetWorkshopsAsync(CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Workshops.AsNoTracking();
        if (CurrentWorkshopId is int workshopId)
            query = query.Where(workshop => workshop.WorkshopId == workshopId);

        return await query
            .OrderBy(workshop => workshop.Number)
            .Select(workshop => new LookupItemDto
            {
                Id = workshop.WorkshopId,
                Name = string.IsNullOrWhiteSpace(workshop.Purpose)
                    ? $"Цех {workshop.Number}"
                    : $"Цех {workshop.Number} - {workshop.Purpose}"
            })
            .ToListAsync(cancellationToken);
    }

    public async Task AddFrameAsync(FrameCardDto frame, CancellationToken cancellationToken = default)
    {
        ApplyCurrentWorkshop(frame);
        EnsureValid(frame);

        _dbContext.Frames.Add(new Frame
        {
            Name = frame.Name.Trim(),
            MaterialTypeId = frame.MaterialTypeId,
            WorkshopId = frame.WorkshopId,
            Weight = frame.Weight,
            Length = frame.Length,
            Width = frame.Width,
            Height = frame.Height,
            ImagePath = NormalizeImagePath(frame.ImagePath)
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateFrameAsync(FrameCardDto frame, CancellationToken cancellationToken = default)
    {
        ApplyCurrentWorkshop(frame);
        EnsureValid(frame);

        var entity = await _dbContext.Frames.FirstOrDefaultAsync(item => item.FrameId == frame.Id, cancellationToken);
        if (entity == null)
            throw new InvalidOperationException("Каркас не найден.");

        EnsureCurrentWorkshopAccess(entity.WorkshopId);

        entity.Name = frame.Name.Trim();
        entity.MaterialTypeId = frame.MaterialTypeId;
        entity.WorkshopId = frame.WorkshopId;
        entity.Weight = frame.Weight;
        entity.Length = frame.Length;
        entity.Width = frame.Width;
        entity.Height = frame.Height;
        entity.ImagePath = NormalizeImagePath(frame.ImagePath);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteFrameAsync(int frameId, CancellationToken cancellationToken = default)
    {
        var frame = await _dbContext.Frames.FirstOrDefaultAsync(item => item.FrameId == frameId, cancellationToken);
        if (frame == null)
            throw new InvalidOperationException("Каркас не найден.");

        EnsureCurrentWorkshopAccess(frame.WorkshopId);

        _dbContext.Frames.Remove(frame);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public int? CurrentWorkshopId => _authService.CurrentUser?.WorkshopId;

    private void ApplyCurrentWorkshop(FrameCardDto frame)
    {
        if (CurrentWorkshopId is not int workshopId)
            return;

        if (frame.WorkshopId > 0 && frame.WorkshopId != workshopId)
            throw new InvalidOperationException("Нельзя добавлять или изменять каркасы другого цеха.");

        frame.WorkshopId = workshopId;
    }

    private void EnsureCurrentWorkshopAccess(int entityWorkshopId)
    {
        if (CurrentWorkshopId is int workshopId && entityWorkshopId != workshopId)
            throw new InvalidOperationException("Нельзя изменять данные другого цеха.");
    }

    private void EnsureValid(FrameCardDto frame)
    {
        var result = _frameCardValidator.Validate(frame);
        if (!result.IsValid)
            throw new InvalidOperationException(result.ErrorMessage ?? "Данные карточки каркаса заполнены некорректно.");
    }

    private static string? NormalizeImagePath(string? imagePath)
    {
        return string.IsNullOrWhiteSpace(imagePath) ? null : imagePath.Trim();
    }

    private static string FormatWorkshop(string number, string? purpose)
    {
        return string.IsNullOrWhiteSpace(purpose)
            ? $"Цех {number}"
            : $"Цех {number} - {purpose}";
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
}
