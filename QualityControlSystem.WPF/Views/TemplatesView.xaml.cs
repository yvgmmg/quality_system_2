using QualityControlSystem.WPF.ViewModels;
using System.Windows.Controls;

namespace QualityControlSystem.WPF.Views;

public partial class TemplatesView : UserControl
{
    public TemplatesView(TemplatesViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
