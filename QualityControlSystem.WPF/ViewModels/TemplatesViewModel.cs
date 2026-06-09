using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QualityControlSystem.WPF.Constants;
using QualityControlSystem.WPF.Dtos;
using QualityControlSystem.WPF.Services.Interfaces;
using QualityControlSystem.WPF.ViewModels.Base;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;

namespace QualityControlSystem.WPF.ViewModels;

public partial class TemplatesViewModel : BaseViewModel, IAsyncInitializable
{
    private readonly ITemplateManagementService _templateService;
    private readonly IDialogService _dialogService;
    private bool _isInitialized;

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
    private string _selectedSideFilter = UiFilterOptions.AllSides;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    public TemplatesViewModel(ITemplateManagementService templateService, IDialogService dialogService)
    {
        _templateService = templateService;
        _dialogService = dialogService;

        SideFilterOptions.Add(UiFilterOptions.AllSides);
        foreach (var side in TemplateSides.All)
            SideFilterOptions.Add(side);

        TemplatesView = CollectionViewSource.GetDefaultView(Templates);
        TemplatesView.Filter = FilterTemplate;
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
        try
        {
            var selectedId = SelectedTemplate?.Id;
            var templates = await _templateService.GetTemplatesAsync();

            Templates.Clear();
            foreach (var template in templates)
                Templates.Add(template);

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

        if (!_dialogService.ShowTemplateDialog(editTemplate, TemplateSides.All, true))
            return;

        IsBusy = true;
        StatusMessage = "Обновление шаблона...";
        try
        {
            await _templateService.UpdateTemplateAsync(editTemplate);
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
            var fileDeleteMessage = await _templateService.DeleteTemplateAsync(SelectedTemplate.Id);
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
            && SelectedSideFilter != UiFilterOptions.AllSides
            && !string.Equals(template.Side, SelectedSideFilter, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
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
