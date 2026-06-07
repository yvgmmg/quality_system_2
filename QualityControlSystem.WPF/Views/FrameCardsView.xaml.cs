using System.Windows.Controls;
using QualityControlSystem.WPF.ViewModels;

namespace QualityControlSystem.WPF.Views;

public partial class FrameCardsView : UserControl
{
    public FrameCardsView(FrameCardsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
