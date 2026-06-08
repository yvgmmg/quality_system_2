using System.Collections.Generic;
using System.Windows;
using QualityControlSystem.WPF.Dtos;
using QualityControlSystem.WPF.ViewModels.Dialogs;

namespace QualityControlSystem.WPF.Views;

public partial class TemplateEditDialog : Window
{
    public TemplateEditDialog(TemplateDto template, IEnumerable<string> sides)
    {
        InitializeComponent();

        var viewModel = new TemplateEditDialogViewModel(template, sides);
        viewModel.CloseRequested += result =>
        {
            DialogResult = result;
            Close();
        };

        DataContext = viewModel;
    }
}
