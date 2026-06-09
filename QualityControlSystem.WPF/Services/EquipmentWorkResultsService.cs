using Microsoft.EntityFrameworkCore;
using QualityControlSystem.Infrastructure;
using QualityControlSystem.Infrastructure.Entities;
using QualityControlSystem.WPF.Constants;
using QualityControlSystem.WPF.Dtos;
using QualityControlSystem.WPF.Services.Interfaces;

namespace QualityControlSystem.WPF.Services;

public class EquipmentWorkResultsService : IEquipmentWorkResultsService
{
    private const string EquipmentSensorCode = "02";

    private readonly AppDbContext _dbContext;
    private readonly IAuthService _authService;

    public EquipmentWorkResultsService(AppDbContext dbContext, IAuthService authService)
    {
        _dbContext = dbContext;
        _authService = authService;
    }

    public async Task<IReadOnlyList<EquipmentWorkResultDto>> GetResultsForEquipmentSpecialistAsync()
    {
        IQueryable<CheckNotification> query = _dbContext.CheckNotifications
            .AsNoTracking()
            .Include(notification => notification.ProductionEquipment);

        if (_authService.CurrentUser?.WorkshopId is int workshopId)
            query = query.Where(notification => notification.ProductionEquipment.WorkshopId == workshopId);

        var rows = await query
            .OrderByDescending(notification => notification.CheckedAt ?? notification.NotificationDate.ToDateTime(TimeOnly.MinValue))
            .ThenByDescending(notification => notification.CheckNotificationId)
            .ToListAsync();

        var equipmentIds = rows
            .Select(notification => notification.ProductionEquipmentId)
            .Distinct()
            .ToList();

        var linkedSensors = await _dbContext.Sensors
            .AsNoTracking()
            .Include(sensor => sensor.SensorType)
            .Include(sensor => sensor.MeasurementUnit)
            .Where(sensor => sensor.ProductionEquipmentId.HasValue
                && equipmentIds.Contains(sensor.ProductionEquipmentId.Value)
                && sensor.SensorType.Code == EquipmentSensorCode)
            .OrderBy(sensor => sensor.Name)
            .Select(sensor => new
            {
                EquipmentId = sensor.ProductionEquipmentId!.Value,
                SensorId = sensor.SensorId,
                SensorName = sensor.Name,
                SensorCode = sensor.SensorType.Code,
                Unit = sensor.MeasurementUnit.Symbol
            })
            .ToListAsync();

        var sensorsByEquipment = linkedSensors
            .GroupBy(sensor => sensor.EquipmentId)
            .ToDictionary(group => group.Key, group => group.ToList());

        return rows.Select(notification =>
        {
            var checkedAt = notification.CheckedAt ?? notification.NotificationDate.ToDateTime(TimeOnly.MinValue);
            var sensorValues = new List<EquipmentSensorValueDto>();

            if (notification.SensorValue.HasValue
                && sensorsByEquipment.TryGetValue(notification.ProductionEquipmentId, out var sensors))
            {
                foreach (var sensor in sensors)
                {
                    sensorValues.Add(new EquipmentSensorValueDto
                    {
                        SensorId = sensor.SensorId,
                        SensorName = sensor.SensorName,
                        SensorCode = sensor.SensorCode,
                        Value = notification.SensorValue.Value.ToString("0.###"),
                        Unit = sensor.Unit,
                        MeasuredAt = checkedAt
                    });
                }
            }

            return new EquipmentWorkResultDto
            {
                Id = notification.CheckNotificationId,
                EquipmentId = notification.ProductionEquipmentId,
                EquipmentName = notification.ProductionEquipment.Name,
                InventoryNumber = notification.ProductionEquipment.InventoryNumber,
                SerialNumber = notification.ProductionEquipment.SerialNumber,
                OkofCode = notification.ProductionEquipment.OkofCode,
                CheckedAt = checkedAt,
                DefectPercentage = notification.DefectPercentage,
                SensorValues = sensorValues
            };
        }).ToList();
    }

    public async Task CreateResultsForFrameInspectionAsync(int frameId, IEnumerable<EdgeInspectionResultDto> inspectionResults)
    {
        var rows = inspectionResults
            .Where(result => result.RecordedAt != default)
            .GroupBy(result => new { result.RecordedAt, result.Id })
            .Select(group => group.First())
            .ToList();

        if (rows.Count == 0)
            return;

        var equipmentIds = await _dbContext.ProductionEquipmentFrames
            .AsNoTracking()
            .Where(link => link.FrameId == frameId)
            .Select(link => link.ProductionEquipmentId)
            .Distinct()
            .ToListAsync();

        if (equipmentIds.Count == 0)
            return;

        var checkedAt = rows.Max(result => result.RecordedAt);
        var sensorValue = rows
            .OrderByDescending(result => result.RecordedAt)
            .Select(result => result.TemperatureC)
            .FirstOrDefault(value => value.HasValue);
        var failedCount = rows.Count(result => !IsPassedStatus(result.Status));
        var defectPercentage = rows.Count == 0
            ? (decimal?)null
            : Math.Round((decimal)failedCount * 100m / rows.Count, 2);

        foreach (var equipmentId in equipmentIds)
        {
            _dbContext.CheckNotifications.Add(new CheckNotification
            {
                ProductionEquipmentId = equipmentId,
                NotificationDate = DateOnly.FromDateTime(checkedAt),
                CheckedAt = checkedAt,
                DefectPercentage = defectPercentage,
                SensorValue = sensorValue.HasValue ? Convert.ToDecimal(sensorValue.Value) : null
            });
        }

        await _dbContext.SaveChangesAsync();
    }

    private static bool IsPassedStatus(string? status)
    {
        return string.Equals(status, InspectionStatuses.Ok, StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, InspectionStatuses.AcceptedRu, StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, InspectionStatuses.Passed, StringComparison.OrdinalIgnoreCase);
    }
}
