using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using QualityControlSystem.Infrastructure;
using QualityControlSystem.Infrastructure.Entities;
using QualityControlSystem.WPF.Dtos;
using QualityControlSystem.WPF.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace QualityControlSystem.WPF.Services;

public class QualityTestService : IQualityTestService
{
    private const string DefaultTestTypeName = "camera_test";

    private readonly AppDbContext _dbContext;

    public QualityTestService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<QualityTestDto>> GetTestsAsync()
    {
        await EnsureDefaultTestTypeAsync();

        var tests = await _dbContext.FrameTestForms
            .AsNoTracking()
            .Include(test => test.TestType)
            .Include(test => test.FrameTestFormFrames)
                .ThenInclude(link => link.Frame)
            .Include(test => test.FrameTestFormTemplates)
                .ThenInclude(link => link.Template)
            .OrderBy(test => test.FrameTestFormId)
            .ToListAsync();

        return tests.Select(test =>
        {
            var frame = test.FrameTestFormFrames.FirstOrDefault()?.Frame;
            var templates = test.FrameTestFormTemplates
                .Select(link => link.Template)
                .OrderBy(template => template.TemplateId)
                .ToList();

            return new QualityTestDto
            {
                Id = test.FrameTestFormId,
                Name = test.Name,
                Description = test.Description,
                FrameId = frame?.FrameId ?? 0,
                FrameName = frame?.Name ?? string.Empty,
                TemplateIds = templates.Select(template => template.TemplateId).ToList(),
                TemplateSummary = string.Join(", ", templates.Select(template => $"{template.Name} ({template.Side})"))
            };
        }).ToList();
    }

    public async Task<IReadOnlyList<LookupItemDto>> GetFramesAsync()
    {
        return await _dbContext.Frames
            .AsNoTracking()
            .OrderBy(frame => frame.Name)
            .Select(frame => new LookupItemDto { Id = frame.FrameId, Name = frame.Name })
            .ToListAsync();
    }

    public async Task<IReadOnlyList<TemplateDto>> GetTemplatesAsync()
    {
        var templates = await _dbContext.Templates
            .AsNoTracking()
            .OrderBy(template => template.Name)
            .Select(template => new
            {
                template.TemplateId,
                template.Name,
                template.ImagePath,
                template.Side
            })
            .ToListAsync();

        return templates.Select(template => new TemplateDto
        {
            Id = template.TemplateId,
            Name = template.Name,
            ImagePath = template.ImagePath,
            Side = template.Side,
            ImagePreview = TemplatePreviewLoader.Load(template.ImagePath)
        }).ToList();
    }

    public async Task AddTestAsync(QualityTestDto test)
    {
        await EnsureDefaultTestTypeAsync();
        ValidateTest(test);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        var defaultTestTypeId = await GetDefaultTestTypeIdAsync();
        var id = await InsertTestFormAsync(test, defaultTestTypeId);
        await SaveLinksAsync(id, test);
        await transaction.CommitAsync();
    }

    public async Task UpdateTestAsync(QualityTestDto test)
    {
        await EnsureDefaultTestTypeAsync();
        ValidateTest(test);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        var defaultTestTypeId = await GetDefaultTestTypeIdAsync();
        await ExecuteNonQueryAsync(
            """
            UPDATE frame_test_form
            SET name = @name,
                description = @description,
                test_type_id = @test_type_id
            WHERE frame_test_form_id = @id;
            """,
            ("id", test.Id),
            ("name", test.Name.Trim()),
            ("description", string.IsNullOrWhiteSpace(test.Description) ? DBNull.Value : test.Description.Trim()),
            ("test_type_id", defaultTestTypeId));

        await SaveLinksAsync(test.Id, test);
        await transaction.CommitAsync();
    }

    public async Task DeleteTestAsync(int id)
    {
        var test = await _dbContext.FrameTestForms.FirstOrDefaultAsync(item => item.FrameTestFormId == id);
        if (test == null)
            return;

        _dbContext.FrameTestForms.Remove(test);
        await _dbContext.SaveChangesAsync();
    }

    private async Task EnsureDefaultTestTypeAsync()
    {
        await ExecuteNonQueryAsync(
            """
            INSERT INTO test_type(name)
            VALUES ('camera_test')
            ON CONFLICT (name) DO NOTHING;
            """);
    }

    private async Task<int> GetDefaultTestTypeIdAsync()
    {
        await using var command = CreateCommand(
            "SELECT test_type_id FROM test_type WHERE name = @name;",
            ("name", DefaultTestTypeName));

        var result = await command.ExecuteScalarAsync();
        if (result == null)
            throw new InvalidOperationException("Не найден технический тип теста camera_test.");

        return Convert.ToInt32(result);
    }

    private async Task<int> InsertTestFormAsync(QualityTestDto test, int testTypeId)
    {
        await using var command = CreateCommand(
            """
            INSERT INTO frame_test_form(name, description, test_type_id)
            VALUES (@name, @description, @test_type_id)
            RETURNING frame_test_form_id;
            """,
            ("name", test.Name.Trim()),
            ("description", string.IsNullOrWhiteSpace(test.Description) ? DBNull.Value : test.Description.Trim()),
            ("test_type_id", testTypeId));

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    private async Task SaveLinksAsync(int testId, QualityTestDto test)
    {
        await ExecuteNonQueryAsync("DELETE FROM frame_test_form_frame WHERE frame_test_form_id = @id;", ("id", testId));
        await ExecuteNonQueryAsync("DELETE FROM frame_test_form_template WHERE frame_test_form_id = @id;", ("id", testId));

        await ExecuteNonQueryAsync(
            "INSERT INTO frame_test_form_frame(frame_test_form_id, frame_id) VALUES (@test_id, @frame_id);",
            ("test_id", testId),
            ("frame_id", test.FrameId));

        foreach (var templateId in test.TemplateIds.Distinct())
        {
            await ExecuteNonQueryAsync(
                """
                INSERT INTO frame_test_form_template(frame_test_form_id, template_id)
                VALUES (@test_id, @template_id)
                ON CONFLICT DO NOTHING;
                """,
                ("test_id", testId),
                ("template_id", templateId));
        }
    }

    private static void ValidateTest(QualityTestDto test)
    {
        if (string.IsNullOrWhiteSpace(test.Name))
            throw new InvalidOperationException("Укажите название теста.");

        if (test.FrameId <= 0)
            throw new InvalidOperationException("Выберите модель каркаса.");

        if (test.TemplateIds.Count == 0)
            throw new InvalidOperationException("Привяжите хотя бы один шаблон.");
    }

    private async Task ExecuteNonQueryAsync(string sql, params (string Name, object? Value)[] parameters)
    {
        await using var command = CreateCommand(sql, parameters);
        await command.ExecuteNonQueryAsync();
    }

    private DbCommand CreateCommand(string sql, params (string Name, object? Value)[] parameters)
    {
        var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = sql;
        if (_dbContext.Database.CurrentTransaction?.GetDbTransaction() is { } transaction)
            command.Transaction = transaction;

        foreach (var (name, value) in parameters)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }

        return command;
    }
}
