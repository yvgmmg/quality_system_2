using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using QualityControlSystem.Infrastructure;
using QualityControlSystem.WPF.Dtos;
using QualityControlSystem.WPF.Services;
using QualityControlSystem.WPF.Services.Interfaces;
using QualityControlSystem.WPF.ViewModels.Base;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;

namespace QualityControlSystem.WPF.ViewModels;

public partial class TemplatesViewModel : BaseViewModel
{
    private static readonly string[] AllowedSides = ["front", "left", "right", "top", "back"];

    private readonly AppDbContext _dbContext;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private ObservableCollection<TemplateDto> _templates = new();

    [ObservableProperty]
    private System.ComponentModel.ICollectionView? _templatesView;

    [ObservableProperty]
    private ObservableCollection<string> _sideFilterOptions = new();

    [ObservableProperty]
    private TemplateDto? _selectedTemplate;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _selectedSideFilter = "Все стороны";

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    public TemplatesViewModel(AppDbContext dbContext, IDialogService dialogService)
    {
        _dbContext = dbContext;
        _dialogService = dialogService;

        SideFilterOptions.Add("Все стороны");
        foreach (var side in AllowedSides)
            SideFilterOptions.Add(side);

        TemplatesView = CollectionViewSource.GetDefaultView(Templates);
        TemplatesView.Filter = FilterTemplate;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var selectedId = SelectedTemplate?.Id;
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
                .ToListAsync();

            Templates.Clear();
            foreach (var template in templates)
            {
                Templates.Add(new TemplateDto
                {
                    Id = template.TemplateId,
                    Name = template.Name,
                    ImagePath = template.ImagePath,
                    Side = template.Side,
                    ImagePreview = TemplatePreviewLoader.Load(template.ImagePath)
                });
            }

            SelectedTemplate = Templates.FirstOrDefault(template => template.Id == selectedId);
            TemplatesView?.Refresh();
            StatusMessage = Templates.Count == 0
                ? "Шаблоны не найдены."
                : $"Шаблонов: {Templates.Count}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка загрузки шаблонов: {GetErrorMessage(ex)}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task EditTemplateAsync()
    {
        if (SelectedTemplate == null)
        {
            StatusMessage = "Выберите шаблон.";
            return;
        }

        var editTemplate = new TemplateDto
        {
            Id = SelectedTemplate.Id,
            Name = SelectedTemplate.Name,
            ImagePath = SelectedTemplate.ImagePath,
            Side = SelectedTemplate.Side,
            ImagePreview = SelectedTemplate.ImagePreview
        };

        if (!_dialogService.ShowTemplateDialog(editTemplate, AllowedSides, true))
            return;

        IsBusy = true;
        StatusMessage = "Обновление шаблона...";
        try
        {
            ValidateTemplate(editTemplate);
            await UpdateTemplateAsync(editTemplate);
            await LoadAsync();
            StatusMessage = "Шаблон обновлен.";
        }
        catch (Exception ex)
        {
            var message = $"Не удалось обновить шаблон: {GetErrorMessage(ex)}";
            StatusMessage = message;
            _dialogService.ShowMessage(message, "Ошибка");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteTemplateAsync()
    {
        if (SelectedTemplate == null)
        {
            StatusMessage = "Выберите шаблон.";
            return;
        }

        if (!_dialogService.ShowConfirm($"Удалить шаблон \"{SelectedTemplate.Name}\"? Он также будет отвязан от тестов."))
            return;

        IsBusy = true;
        StatusMessage = "Удаление шаблона...";
        try
        {
            var deletedTemplatePath = await DeleteTemplateRecordAsync(SelectedTemplate.Id);
            var fileDeleteMessage = DeleteTemplateFiles(deletedTemplatePath);
            SelectedTemplate = null;
            await LoadAsync();
            if (!string.IsNullOrWhiteSpace(fileDeleteMessage))
            {
                StatusMessage = fileDeleteMessage;
                return;
            }
            StatusMessage = "Шаблон удален.";
        }
        catch (Exception ex)
        {
            var message = $"Не удалось удалить шаблон: {GetErrorMessage(ex)}";
            StatusMessage = message;
            _dialogService.ShowMessage(message, "Ошибка");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task UpdateTemplateAsync(TemplateDto template)
    {
        var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync();

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
        await command.ExecuteNonQueryAsync();
    }

    private async Task<string?> DeleteTemplateRecordAsync(int templateId)
    {
        string? imagePath = null;
        var canDeleteFiles = false;

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync();

        await using (var select = connection.CreateCommand())
        {
            select.Transaction = _dbContext.Database.CurrentTransaction?.GetDbTransaction();
            select.CommandText = "SELECT image_path FROM template WHERE template_id = @id;";
            AddParameter(select, "id", templateId);
            imagePath = await select.ExecuteScalarAsync() as string;
        }

        await using (var unlink = connection.CreateCommand())
        {
            unlink.Transaction = _dbContext.Database.CurrentTransaction?.GetDbTransaction();
            unlink.CommandText = "DELETE FROM frame_test_form_template WHERE template_id = @id;";
            AddParameter(unlink, "id", templateId);
            await unlink.ExecuteNonQueryAsync();
        }

        await using (var delete = connection.CreateCommand())
        {
            delete.Transaction = _dbContext.Database.CurrentTransaction?.GetDbTransaction();
            delete.CommandText = "DELETE FROM template WHERE template_id = @id;";
            AddParameter(delete, "id", templateId);
            await delete.ExecuteNonQueryAsync();
        }

        if (!string.IsNullOrWhiteSpace(imagePath))
        {
            await using var count = connection.CreateCommand();
            count.Transaction = _dbContext.Database.CurrentTransaction?.GetDbTransaction();
            count.CommandText = "SELECT COUNT(*) FROM template WHERE image_path = @image_path;";
            AddParameter(count, "image_path", imagePath);
            canDeleteFiles = Convert.ToInt32(await count.ExecuteScalarAsync()) == 0;
        }

        await transaction.CommitAsync();
        return canDeleteFiles ? imagePath : null;
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

    private bool FilterTemplate(object item)
    {
        if (item is not TemplateDto template)
            return false;

        if (!string.IsNullOrWhiteSpace(SearchText)
            && !template.Name.Contains(SearchText.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(SelectedSideFilter)
            && SelectedSideFilter != "Все стороны"
            && !string.Equals(template.Side, SelectedSideFilter, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private static void ValidateTemplate(TemplateDto template)
    {
        if (string.IsNullOrWhiteSpace(template.Name))
            throw new InvalidOperationException("Укажите имя шаблона.");

        if (!AllowedSides.Contains(template.Side?.Trim().ToLowerInvariant()))
            throw new InvalidOperationException("Выберите сторону каркаса.");
    }

    private static void AddParameter(IDbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    private static string GetErrorMessage(Exception exception)
    {
        var current = exception;
        while (current.InnerException != null)
            current = current.InnerException;

        return current.Message;
    }

    partial void OnSearchTextChanged(string value) => TemplatesView?.Refresh();

    partial void OnSelectedSideFilterChanged(string value) => TemplatesView?.Refresh();
}
