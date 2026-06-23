using Microsoft.EntityFrameworkCore;
using QualityControlSystem.Infrastructure;
using QualityControlSystem.Infrastructure.Entities;
using QualityControlSystem.WPF.Constants;
using QualityControlSystem.WPF.Dtos;
using QualityControlSystem.WPF.Services.Interfaces;
using System.Globalization;
using System.IO;
using System.Text;

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
        return await LoadResultsForEquipmentSpecialistAsync(CancellationToken.None);
    }

    public async Task<int> ClearResultsForEquipmentSpecialistAsync(CancellationToken cancellationToken = default)
    {
        IQueryable<CheckNotification> query = _dbContext.CheckNotifications
            .Include(notification => notification.ProductionEquipment);

        if (_authService.CurrentUser?.WorkshopId is int workshopId)
            query = query.Where(notification => notification.ProductionEquipment.WorkshopId == workshopId);

        var notifications = await query.ToListAsync(cancellationToken);
        if (notifications.Count == 0)
            return 0;

        _dbContext.CheckNotifications.RemoveRange(notifications);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return notifications.Count;
    }

    public async Task CreateEquipmentWorkResultsReportAsync(
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
            throw new InvalidOperationException("Путь для сохранения отчета не указан.");

        var results = await LoadResultsForEquipmentSpecialistAsync(cancellationToken);
        if (results.Count == 0)
            throw new InvalidOperationException("Нет уведомлений для формирования отчета.");

        var reportText = BuildEquipmentWorkResultsReport(results);
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        await File.WriteAllTextAsync(outputPath, reportText, Encoding.UTF8, cancellationToken);
    }

    private async Task<IReadOnlyList<EquipmentWorkResultDto>> LoadResultsForEquipmentSpecialistAsync(CancellationToken cancellationToken)
    {
        IQueryable<CheckNotification> query = _dbContext.CheckNotifications
            .AsNoTracking()
            .Include(notification => notification.ProductionEquipment);

        if (_authService.CurrentUser?.WorkshopId is int workshopId)
            query = query.Where(notification => notification.ProductionEquipment.WorkshopId == workshopId);

        var rows = await query
            .OrderByDescending(notification => notification.CheckedAt ?? notification.NotificationDate.ToDateTime(TimeOnly.MinValue))
            .ThenByDescending(notification => notification.CheckNotificationId)
            .ToListAsync(cancellationToken);

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
            .ToListAsync(cancellationToken);

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

    private static string BuildEquipmentWorkResultsReport(IReadOnlyList<EquipmentWorkResultDto> results)
    {
        var builder = new StringBuilder();
        var equipmentCount = results
            .Select(result => result.EquipmentId)
            .Distinct()
            .Count();
        var defectValues = results
            .Where(result => result.DefectPercentage.HasValue)
            .Select(result => result.DefectPercentage!.Value)
            .ToList();

        builder.AppendLine("ОТЧЕТ О РЕЗУЛЬТАТАХ РАБОТЫ ОБОРУДОВАНИЯ");
        builder.AppendLine($"Дата составления: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
        builder.AppendLine();
        builder.AppendLine("Итоги");
        builder.AppendLine($"Всего уведомлений: {results.Count}");
        builder.AppendLine($"Оборудования в отчете: {equipmentCount}");
        builder.AppendLine($"Средний процент брака: {ReportDecimalPercent(defectValues.Count == 0 ? null : defectValues.Average())}");
        builder.AppendLine($"Максимальный процент брака: {ReportDecimalPercent(defectValues.Count == 0 ? null : defectValues.Max())}");
        builder.AppendLine();
        builder.AppendLine("Результаты работы оборудования");
        builder.AppendLine("№\tВремя проверки\tОборудование\tИнвентарный номер\tСерийный номер\tОКОФ\tПроцент брака\tДатчики");

        for (var index = 0; index < results.Count; index++)
        {
            var result = results[index];
            builder.AppendLine(
                $"{index + 1}\t" +
                $"{result.CheckedAt:dd.MM.yyyy HH:mm:ss}\t" +
                $"{ReportText(result.EquipmentName)}\t" +
                $"{ReportText(result.InventoryNumber)}\t" +
                $"{ReportText(result.SerialNumber)}\t" +
                $"{ReportText(result.OkofCode)}\t" +
                $"{ReportDecimalPercent(result.DefectPercentage)}\t" +
                $"{ReportSensorValues(result.SensorValues)}");
        }

        return builder.ToString();
    }

    private static string ReportText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "не указано" : value.Trim();
    }

    private static string ReportDecimalPercent(decimal? value)
    {
        return value.HasValue
            ? string.Create(CultureInfo.GetCultureInfo("ru-RU"), $"{value.Value:0.##}%")
            : "не указано";
    }

    private static string ReportSensorValues(IReadOnlyCollection<EquipmentSensorValueDto> sensors)
    {
        if (sensors.Count == 0)
            return "не указаны";

        return string.Join("; ", sensors.Select(sensor =>
        {
            var measuredAt = sensor.MeasuredAt.HasValue
                ? $" ({sensor.MeasuredAt.Value:dd.MM.yyyy HH:mm:ss})"
                : string.Empty;
            var unit = string.IsNullOrWhiteSpace(sensor.Unit) ? string.Empty : $" {sensor.Unit}";

            return $"{ReportText(sensor.SensorName)} [{ReportText(sensor.SensorCode)}]: {ReportText(sensor.Value)}{unit}{measuredAt}";
        }));
    }
}
