using QualityControlSystem.WPF.ViewModels;
using System.Windows.Controls;

namespace QualityControlSystem.WPF.Views
{
    public partial class OperatorControlView : UserControl
    {
        public OperatorControlView(OperatorControlViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
