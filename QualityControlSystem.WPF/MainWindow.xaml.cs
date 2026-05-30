using QualityControlSystem.WPF.ViewModels;
using System.Windows;

namespace QualityControlSystem.WPF.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}