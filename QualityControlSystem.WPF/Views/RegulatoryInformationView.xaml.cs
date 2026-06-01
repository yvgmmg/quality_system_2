using System.Windows.Controls;
using QualityControlSystem.WPF.ViewModels;

namespace QualityControlSystem.WPF.Views;

public partial class RegulatoryInformationView : UserControl
{
    public RegulatoryInformationView(RegulatoryInformationViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
