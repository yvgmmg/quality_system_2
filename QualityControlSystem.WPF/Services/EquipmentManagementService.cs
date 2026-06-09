using Microsoft.EntityFrameworkCore;
using QualityControlSystem.Infrastructure;
using QualityControlSystem.Infrastructure.Entities;
using QualityControlSystem.WPF.Dtos;
using QualityControlSystem.WPF.Services.Interfaces;
using QualityControlSystem.WPF.Validation;

namespace QualityControlSystem.WPF.Services;

public class EquipmentManagementService : IEquipmentManagementService
{
    private const string EquipmentSensorCode = "02";
    private readonly AppDbContext _dbContext;
    private readonly IAuthService _authService;
    private readonly IEquipmentValidator _equipmentValidator;

    public EquipmentManagementService(
        AppDbContext dbContext,
        IAuthService authService,
        IEquipmentValidator equipmentValidator)
    {
        _dbContext = dbContext;
        _authService = authService;
        _equipmentValidator = equipmentValidator;
    }

    public async Task<IReadOnlyList<LookupItemDto>> GetWorkshopOptionsAsync()
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
            .ToListAsync();
    }

    public async Task<IReadOnlyList<ProductionEquipmentDto>> GetEquipmentAsync()
    {
        IQueryable<ProductionEquipment> query = _dbContext.ProductionEquipments
            .AsNoTracking()
            .Include(item => item.Workshop);

        if (CurrentWorkshopId is int workshopId)
            query = query.Where(item => item.WorkshopId == workshopId);

        return await query
            .OrderBy(item => item.ProductionEquipmentId)
            .Select(item => new ProductionEquipmentDto
            {
                Id = item.ProductionEquipmentId,
                Name = item.Name,
                SerialNumber = item.SerialNumber,
                OkofCode = item.OkofCode,
                InventoryNumber = item.InventoryNumber,
                WorkshopId = item.WorkshopId,
                WorkshopName = string.IsNullOrWhiteSpace(item.Workshop.Purpose)
                    ? $"Цех {item.Workshop.Number}"
                    : $"Цех {item.Workshop.Number} - {item.Workshop.Purpose}"
            })
            .ToListAsync();
    }

    public async Task AddEquipmentAsync(ProductionEquipmentDto equipment)
    {
        ApplyCurrentWorkshop(equipment);
        EnsureValid(equipment);
        _dbContext.ProductionEquipments.Add(new ProductionEquipment
        {
            Name = equipment.Name.Trim(),
            SerialNumber = NormalizeOptionalText(equipment.SerialNumber),
            OkofCode = equipment.OkofCode.Trim(),
            InventoryNumber = equipment.InventoryNumber.Trim(),
            WorkshopId = equipment.WorkshopId
        });

        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateEquipmentAsync(ProductionEquipmentDto equipment)
    {
        ApplyCurrentWorkshop(equipment);
        EnsureValid(equipment);
        var entity = await _dbContext.ProductionEquipments
            .FirstOrDefaultAsync(item => item.ProductionEquipmentId == equipment.Id);

        if (entity == null)
            throw new InvalidOperationException("Оборудование не найдено.");

        EnsureCurrentWorkshopAccess(entity.WorkshopId);

        entity.Name = equipment.Name.Trim();
        entity.SerialNumber = NormalizeOptionalText(equipment.SerialNumber);
        entity.OkofCode = equipment.OkofCode.Trim();
        entity.InventoryNumber = equipment.InventoryNumber.Trim();
        entity.WorkshopId = equipment.WorkshopId;

        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteEquipmentAsync(int equipmentId)
    {
        var entity = await _dbContext.ProductionEquipments
            .FirstOrDefaultAsync(item => item.ProductionEquipmentId == equipmentId);

        if (entity == null)
            throw new InvalidOperationException("Оборудование не найдено.");

        EnsureCurrentWorkshopAccess(entity.WorkshopId);

        _dbContext.ProductionEquipments.Remove(entity);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<LookupItemDto>> GetAvailableFramesAsync()
    {
        var query = _dbContext.Frames.AsNoTracking();
        if (CurrentWorkshopId is int workshopId)
            query = query.Where(frame => frame.WorkshopId == workshopId);

        return await query
            .OrderBy(frame => frame.Name)
            .Select(frame => new LookupItemDto
            {
                Id = frame.FrameId,
                Name = frame.Name
            })
            .ToListAsync();
    }

    public async Task<IReadOnlyList<LookupItemDto>> GetFramesForEquipmentAsync(int equipmentId)
    {
        return await _dbContext.ProductionEquipmentFrames
            .AsNoTracking()
            .Where(link => link.ProductionEquipmentId == equipmentId)
            .OrderBy(link => link.Frame.Name)
            .Select(link => new LookupItemDto
            {
                Id = link.FrameId,
                Name = link.Frame.Name
            })
            .ToListAsync();
    }

    public async Task UpdateFramesForEquipmentAsync(int equipmentId, IReadOnlyCollection<int> frameIds)
    {
        await EnsureEquipmentExistsAsync(equipmentId);

        var requestedIds = frameIds.Distinct().ToHashSet();
        var existingFrameIds = await _dbContext.Frames
            .AsNoTracking()
            .Where(frame => requestedIds.Contains(frame.FrameId))
            .Select(frame => frame.FrameId)
            .ToListAsync();

        if (existingFrameIds.Count != requestedIds.Count)
            throw new InvalidOperationException("Один или несколько выбранных каркасов не найдены.");

        if (CurrentWorkshopId is int workshopId)
        {
            var foreignWorkshopFrameExists = await _dbContext.Frames
                .AsNoTracking()
                .AnyAsync(frame => requestedIds.Contains(frame.FrameId) && frame.WorkshopId != workshopId);

            if (foreignWorkshopFrameExists)
                throw new InvalidOperationException("Нельзя привязывать к оборудованию каркасы другого цеха.");
        }

        var existingLinks = await _dbContext.ProductionEquipmentFrames
            .Where(link => link.ProductionEquipmentId == equipmentId)
            .ToListAsync();

        _dbContext.ProductionEquipmentFrames.RemoveRange(
            existingLinks.Where(link => !requestedIds.Contains(link.FrameId)));

        var existingIds = existingLinks.Select(link => link.FrameId).ToHashSet();
        var linksToAdd = requestedIds
            .Where(frameId => !existingIds.Contains(frameId))
            .Select(frameId => new ProductionEquipmentFrame
            {
                ProductionEquipmentId = equipmentId,
                FrameId = frameId
            });

        _dbContext.ProductionEquipmentFrames.AddRange(linksToAdd);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<EquipmentSensorDto>> GetAvailableEquipmentSensorsAsync()
    {
        var query = _dbContext.Sensors
            .AsNoTracking()
            .Include(sensor => sensor.SensorType)
            .Include(sensor => sensor.MeasurementUnit)
            .Where(sensor => sensor.SensorType.Code == EquipmentSensorCode);

        if (CurrentWorkshopId is int workshopId)
            query = query.Where(sensor => sensor.WorkshopId == workshopId);

        return await query
            .OrderBy(sensor => sensor.Name)
            .Select(sensor => new EquipmentSensorDto
            {
                Id = sensor.SensorId,
                Name = sensor.Name,
                Code = sensor.SensorType.Code,
                Unit = sensor.MeasurementUnit.Symbol
            })
            .ToListAsync();
    }

    public async Task<IReadOnlyList<EquipmentSensorDto>> GetSensorsForEquipmentAsync(int equipmentId)
    {
        return await _dbContext.Sensors
            .AsNoTracking()
            .Include(sensor => sensor.SensorType)
            .Include(sensor => sensor.MeasurementUnit)
            .Where(sensor => sensor.ProductionEquipmentId == equipmentId && sensor.SensorType.Code == EquipmentSensorCode)
            .OrderBy(sensor => sensor.Name)
            .Select(sensor => new EquipmentSensorDto
            {
                Id = sensor.SensorId,
                Name = sensor.Name,
                Code = sensor.SensorType.Code,
                Unit = sensor.MeasurementUnit.Symbol
            })
            .ToListAsync();
    }

    public async Task UpdateSensorsForEquipmentAsync(int equipmentId, IReadOnlyCollection<int> sensorIds)
    {
        await EnsureEquipmentExistsAsync(equipmentId);

        var requestedIds = sensorIds.Distinct().ToHashSet();
        var selectedSensors = await _dbContext.Sensors
            .Include(sensor => sensor.SensorType)
            .Where(sensor => requestedIds.Contains(sensor.SensorId))
            .ToListAsync();

        if (selectedSensors.Count != requestedIds.Count)
            throw new InvalidOperationException("Один или несколько выбранных датчиков не найдены.");

        if (selectedSensors.Any(sensor => sensor.SensorType.Code != EquipmentSensorCode))
            throw new InvalidOperationException("К оборудованию можно привязывать только датчики с кодом 02.");

        if (CurrentWorkshopId is int workshopId
            && selectedSensors.Any(sensor => sensor.WorkshopId != workshopId))
            throw new InvalidOperationException("Нельзя привязывать к оборудованию датчики другого цеха.");

        if (selectedSensors.Any(sensor => sensor.ProductionEquipmentId.HasValue && sensor.ProductionEquipmentId.Value != equipmentId))
            throw new InvalidOperationException("Датчик уже привязан к другому оборудованию.");

        var previouslyLinkedSensors = await _dbContext.Sensors
            .Include(sensor => sensor.SensorType)
            .Where(sensor => sensor.ProductionEquipmentId == equipmentId)
            .ToListAsync();

        foreach (var sensor in previouslyLinkedSensors)
            sensor.ProductionEquipmentId = null;

        foreach (var sensor in selectedSensors)
            sensor.ProductionEquipmentId = equipmentId;

        var frameSensorsWithEquipment = await _dbContext.Sensors
            .Include(sensor => sensor.SensorType)
            .Where(sensor => sensor.ProductionEquipmentId != null && sensor.SensorType.Code != EquipmentSensorCode)
            .ToListAsync();

        foreach (var sensor in frameSensorsWithEquipment)
            sensor.ProductionEquipmentId = null;

        await _dbContext.SaveChangesAsync();
    }

    private int? CurrentWorkshopId => _authService.CurrentUser?.WorkshopId;

    private void ApplyCurrentWorkshop(ProductionEquipmentDto equipment)
    {
        if (CurrentWorkshopId is not int workshopId)
            return;

        if (equipment.WorkshopId > 0 && equipment.WorkshopId != workshopId)
            throw new InvalidOperationException("Нельзя добавлять или изменять оборудование другого цеха.");

        equipment.WorkshopId = workshopId;
    }

    private async Task EnsureEquipmentExistsAsync(int equipmentId)
    {
        var equipment = await _dbContext.ProductionEquipments
            .AsNoTracking()
            .Where(item => item.ProductionEquipmentId == equipmentId)
            .Select(item => new { item.WorkshopId })
            .FirstOrDefaultAsync();

        if (equipment == null)
            throw new InvalidOperationException("Оборудование не найдено.");

        EnsureCurrentWorkshopAccess(equipment.WorkshopId);
    }

    private void EnsureCurrentWorkshopAccess(int entityWorkshopId)
    {
        if (CurrentWorkshopId is int workshopId && entityWorkshopId != workshopId)
            throw new InvalidOperationException("Нельзя изменять данные другого цеха.");
    }

    private void EnsureValid(ProductionEquipmentDto equipment)
    {
        var result = _equipmentValidator.Validate(equipment);
        if (!result.IsValid)
            throw new InvalidOperationException(result.ErrorMessage ?? "Данные оборудования заполнены некорректно.");
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
