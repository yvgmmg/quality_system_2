using System.Windows.Controls;
using QualityControlSystem.WPF.ViewModels;

namespace QualityControlSystem.WPF.Views;

public partial class EquipmentManagementView : UserControl
{
    public EquipmentManagementView(EquipmentManagementViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
