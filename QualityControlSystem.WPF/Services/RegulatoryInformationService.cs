using System.Data;
using Microsoft.EntityFrameworkCore;
using QualityControlSystem.Infrastructure;
using QualityControlSystem.Infrastructure.Enums;
using QualityControlSystem.WPF.Models;
using QualityControlSystem.WPF.Services.Interfaces;

namespace QualityControlSystem.WPF.Services;

public class RegulatoryInformationService : IRegulatoryInformationService
{
    private readonly AppDbContext _dbContext;

    public RegulatoryInformationService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<RegulatoryInformationDto>> GetAllAsync()
    {
        return SearchAsync(null, null);
    }

    public async Task<List<RegulatoryInformationDto>> SearchAsync(string? searchText, string? type)
    {
        await using var command = _dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = """
            SELECT regulatory_information_id,
                   "name",
                   "type"::text,
                   description,
                   min_value,
                   max_value,
                   measurement::text,
                   approval_date,
                   end_date
            FROM public.regulatory_information
            WHERE (CAST(@search_text AS text) IS NULL OR LOWER("name") LIKE CAST(@search_text AS text))
              AND (CAST(@type_filter AS text) IS NULL OR "type"::text = CAST(@type_filter AS text))
            ORDER BY "name";
            """;

        AddParameter(command, "@search_text", string.IsNullOrWhiteSpace(searchText) ? DBNull.Value : $"%{searchText.Trim().ToLower()}%");
        AddParameter(command, "@type_filter", string.IsNullOrWhiteSpace(type) ? DBNull.Value : type);

        if (command.Connection!.State != ConnectionState.Open)
            await command.Connection.OpenAsync();

        var items = new List<RegulatoryInformationDto>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            items.Add(new RegulatoryInformationDto
            {
                RegulatoryInformationId = reader.GetInt32(0),
                Name = reader.GetString(1),
                Type = reader.GetString(2),
                Description = reader.IsDBNull(3) ? null : reader.GetString(3),
                MinValue = reader.IsDBNull(4) ? null : reader.GetFloat(4),
                MaxValue = reader.IsDBNull(5) ? null : reader.GetFloat(5),
                Measurement = FromDatabaseMeasurement(reader.GetString(6)),
                StartDate = reader.IsDBNull(7) ? DateTime.Today : reader.GetFieldValue<DateOnly>(7).ToDateTime(TimeOnly.MinValue),
                EndDate = reader.IsDBNull(8) ? null : reader.GetFieldValue<DateOnly>(8).ToDateTime(TimeOnly.MinValue)
            });
        }

        return items;
    }

    public async Task AddAsync(RegulatoryInformationDto dto)
    {
        Validate(dto);

        var databaseType = ToDatabaseType(dto.Type);
        var measurement = ToDatabaseMeasurement(dto.Measurement);
        var approvalDate = DateOnly.FromDateTime(dto.StartDate);
        var endDate = dto.EndDate.HasValue ? DateOnly.FromDateTime(dto.EndDate.Value) : (DateOnly?)null;

        await _dbContext.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO public.regulatory_information
                (""name"", ""type"", description, min_value, max_value, approval_date, end_date, measurement)
            VALUES
                ({dto.Name}, CAST({databaseType} AS public.source), {dto.Description}, {dto.MinValue}, {dto.MaxValue},
                 {approvalDate}, {endDate}, CAST({measurement} AS public.measurement_unit))");
    }

    public async Task UpdateAsync(RegulatoryInformationDto dto)
    {
        Validate(dto);

        var exists = await _dbContext.RegulatoryInformations
            .AnyAsync(item => item.RegulatoryInformationId == dto.RegulatoryInformationId);
        if (!exists)
            throw new InvalidOperationException("Запись НСИ не найдена.");

        var databaseType = ToDatabaseType(dto.Type);
        var measurement = ToDatabaseMeasurement(dto.Measurement);
        var approvalDate = DateOnly.FromDateTime(dto.StartDate);
        var endDate = dto.EndDate.HasValue ? DateOnly.FromDateTime(dto.EndDate.Value) : (DateOnly?)null;

        await _dbContext.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE public.regulatory_information
            SET ""name"" = {dto.Name},
                ""type"" = CAST({databaseType} AS public.source),
                description = {dto.Description},
                min_value = {dto.MinValue},
                max_value = {dto.MaxValue},
                approval_date = {approvalDate},
                end_date = {endDate},
                measurement = CAST({measurement} AS public.measurement_unit)
            WHERE regulatory_information_id = {dto.RegulatoryInformationId}");
    }

    public async Task DeleteAsync(int id)
    {
        await _dbContext.Database.ExecuteSqlInterpolatedAsync($@"
            DELETE FROM public.regulatory_information
            WHERE regulatory_information_id = {id}");
    }

    private static void AddParameter(IDbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.DbType = DbType.String;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static void Validate(RegulatoryInformationDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new InvalidOperationException("Название обязательно.");

        if (dto.MinValue.HasValue && dto.MaxValue.HasValue && dto.MinValue.Value > dto.MaxValue.Value)
            throw new InvalidOperationException("Минимальное значение не должно быть больше максимального.");

        if (dto.EndDate.HasValue && dto.EndDate.Value < dto.StartDate)
            throw new InvalidOperationException("Дата окончания не должна быть раньше даты начала.");

        ToDatabaseType(dto.Type);
        ToDatabaseMeasurement(dto.Measurement);
    }

    public static string ToDatabaseMeasurement(MeasurementUnit measurement)
    {
        var preferredValue = ToPreferredDatabaseMeasurement(measurement);
        if (preferredValue != null)
            return preferredValue;

        return measurement.ToString() switch
        {
            "GradC" => "В°C",
            "Grad" => "В°",
            "Empty" => "%",
            "m_s" => "Рј/СЃ",
            "m_kv" => "РјВІ",
            "m_kub" => "РјВі",
            "bezrazm" => "Р±РµР·СЂР°Р·Рј.",
            var value => value
        };
    }

    public static MeasurementUnit FromDatabaseMeasurement(string value)
    {
        if (TryParseDatabaseMeasurement(value, out var parsedMeasurement))
            return parsedMeasurement;

        var enumName = value switch
        {
            "В°C" => "GradC",
            "В°" => "Grad",
            "%" => "Empty",
            "Рј/СЃ" => "m_s",
            "РјВІ" => "m_kv",
            "РјВі" => "m_kub",
            "Р±РµР·СЂР°Р·Рј." => "bezrazm",
            _ => value
        };

        if (Enum.TryParse<MeasurementUnit>(enumName, out var measurement))
            return measurement;

        throw new InvalidOperationException($"Единица измерения '{value}' не поддерживается приложением.");
    }

    private static string? ToPreferredDatabaseMeasurement(MeasurementUnit measurement)
    {
        return measurement.ToString() switch
        {
            "РјРј" => "мм",
            "СЃРј" => "см",
            "Рј" => "м",
            "Рі" => "г",
            "РєРі" => "кг",
            "С‚" => "т",
            "GradC" => "градус_цельсия",
            "Grad" => "градус",
            "Empty" => "процент",
            "С€С‚" => "шт",
            "Рќ" => "Н",
            "РњРџР°" => "МПа",
            "Р’" => "В",
            "Рђ" => "А",
            "m_s" => "метр_в_секунду",
            "m_kv" => "квадратный_метр",
            "m_kub" => "кубический_метр",
            "bezrazm" => "безразмерная",
            _ => null
        };
    }

    private static bool TryParseDatabaseMeasurement(string value, out MeasurementUnit measurement)
    {
        var enumName = value switch
        {
            "мм" => "РјРј",
            "см" => "СЃРј",
            "м" => "Рј",
            "г" => "Рі",
            "кг" => "РєРі",
            "т" => "С‚",
            "°C" or "градус_цельсия" => "GradC",
            "°" or "градус" => "Grad",
            "%" or "процент" => "Empty",
            "шт" => "С€С‚",
            "Н" => "Рќ",
            "МПа" => "РњРџР°",
            "В" => "Р’",
            "А" => "Рђ",
            "м/с" or "метр_в_секунду" => "m_s",
            "м²" or "квадратный_метр" => "m_kv",
            "м³" or "кубический_метр" => "m_kub",
            "безразм." or "безразмерная" => "bezrazm",
            _ => value
        };

        return Enum.TryParse(enumName, out measurement);
    }

    private static string ToDatabaseType(string? type)
    {
        return type?.Trim().ToLowerInvariant() switch
        {
            "equipment" => "equipment",
            "frame" => "frame",
            _ => throw new InvalidOperationException("Выбран некорректный тип / источник.")
        };
    }
}
