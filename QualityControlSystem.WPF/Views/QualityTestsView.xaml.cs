using QualityControlSystem.WPF.ViewModels;
using System.Windows.Controls;

namespace QualityControlSystem.WPF.Views;

public partial class QualityTestsView : UserControl
{
    public QualityTestsView(QualityTestsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
