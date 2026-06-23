using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using QualityControlSystem.Infrastructure;
using QualityControlSystem.WPF.Constants;
using QualityControlSystem.WPF.Dtos;
using QualityControlSystem.WPF.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace QualityControlSystem.WPF.Services;

public class TemplateManagementService : ITemplateManagementService
{
    private readonly AppDbContext _dbContext;

    public TemplateManagementService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<TemplateDto>> GetTemplatesAsync(CancellationToken cancellationToken = default)
    {
        var templates = await _dbContext.Templates
            .AsNoTracking()
            .OrderBy(template => template.TemplateId)
            .Select(template => new
            {
                template.TemplateId,
                template.Name,
                template.ImagePath,
                template.Side
            })
            .ToListAsync(cancellationToken);

        return templates.Select(template => new TemplateDto
        {
            Id = template.TemplateId,
            Name = template.Name,
            ImagePath = template.ImagePath,
            Side = template.Side,
            ImagePreview = TemplatePreviewLoader.Load(template.ImagePath)
        }).ToList();
    }

    public async Task UpdateTemplateAsync(TemplateDto template, CancellationToken cancellationToken = default)
    {
        ValidateTemplate(template);

        var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE template
            SET name = @name,
                side = CAST(@side AS template_side)
            WHERE template_id = @id;
            """;
        AddParameter(command, "id", template.Id);
        AddParameter(command, "name", template.Name.Trim());
        AddParameter(command, "side", template.Side.Trim().ToLowerInvariant());

        var affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);
        if (affectedRows == 0)
            throw new InvalidOperationException("Шаблон не найден.");
    }

    public async Task<string?> DeleteTemplateAsync(int templateId, CancellationToken cancellationToken = default)
    {
        string? imagePath = null;
        var canDeleteFiles = false;

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using (var select = connection.CreateCommand())
        {
            select.Transaction = _dbContext.Database.CurrentTransaction?.GetDbTransaction();
            select.CommandText = "SELECT image_path FROM template WHERE template_id = @id;";
            AddParameter(select, "id", templateId);
            imagePath = await select.ExecuteScalarAsync(cancellationToken) as string;
        }

        if (imagePath == null)
            throw new InvalidOperationException("Шаблон не найден.");

        await using (var unlink = connection.CreateCommand())
        {
            unlink.Transaction = _dbContext.Database.CurrentTransaction?.GetDbTransaction();
            unlink.CommandText = "DELETE FROM frame_test_form_template WHERE template_id = @id;";
            AddParameter(unlink, "id", templateId);
            await unlink.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var delete = connection.CreateCommand())
        {
            delete.Transaction = _dbContext.Database.CurrentTransaction?.GetDbTransaction();
            delete.CommandText = "DELETE FROM template WHERE template_id = @id;";
            AddParameter(delete, "id", templateId);
            await delete.ExecuteNonQueryAsync(cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(imagePath))
        {
            await using var count = connection.CreateCommand();
            count.Transaction = _dbContext.Database.CurrentTransaction?.GetDbTransaction();
            count.CommandText = "SELECT COUNT(*) FROM template WHERE image_path = @image_path;";
            AddParameter(count, "image_path", imagePath);
            canDeleteFiles = Convert.ToInt32(await count.ExecuteScalarAsync(cancellationToken)) == 0;
        }

        await transaction.CommitAsync(cancellationToken);
        return canDeleteFiles ? DeleteTemplateFiles(imagePath) : null;
    }

    private static void ValidateTemplate(TemplateDto template)
    {
        if (string.IsNullOrWhiteSpace(template.Name))
            throw new InvalidOperationException("Укажите имя шаблона.");

        if (!TemplateSides.All.Contains(template.Side?.Trim().ToLowerInvariant()))
            throw new InvalidOperationException("Выберите сторону каркаса.");
    }

    private static string? DeleteTemplateFiles(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
            return null;

        try
        {
            var absolutePath = Path.GetFullPath(imagePath);
            if (Directory.Exists(absolutePath))
            {
                Directory.Delete(absolutePath, recursive: true);
                return "Файлы шаблона удалены с диска.";
            }

            if (File.Exists(absolutePath))
            {
                File.Delete(absolutePath);
                return "Файл шаблона удален с диска.";
            }
        }
        catch (Exception ex)
        {
            return $"Шаблон удален из БД, но файлы не удалось удалить: {ex.Message}";
        }

        return null;
    }

    private static void AddParameter(IDbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }
}
