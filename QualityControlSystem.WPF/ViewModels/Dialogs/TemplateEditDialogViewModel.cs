using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QualityControlSystem.WPF.Dtos;
using QualityControlSystem.WPF.ViewModels.Base;

namespace QualityControlSystem.WPF.ViewModels.Dialogs;

public partial class TemplateEditDialogViewModel : BaseViewModel
{
    private readonly TemplateDto _template;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string? _selectedSide;

    [ObservableProperty]
    private string _imagePath = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public TemplateEditDialogViewModel(TemplateDto template, IEnumerable<string> sides)
    {
        _template = template;
        Sides = new ObservableCollection<string>(sides);
        Name = template.Name;
        SelectedSide = template.Side;
        ImagePath = template.ImagePath;
    }

    public ObservableCollection<string> Sides { get; }

    public event Action<bool?>? CloseRequested;

    [RelayCommand]
    private void Save()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "Укажите имя шаблона.";
            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedSide))
        {
            ErrorMessage = "Выберите сторону каркаса.";
            return;
        }

        _template.Name = Name.Trim();
        _template.Side = SelectedSide;
        CloseRequested?.Invoke(true);
    }
}
