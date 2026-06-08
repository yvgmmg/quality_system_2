using System.Windows;
using QualityControlSystem.WPF.Dtos;
using QualityControlSystem.WPF.ViewModels.Dialogs;

namespace QualityControlSystem.WPF.Views
{
    public partial class UserEditDialog : Window
    {
        public UserEditDialog(UserProfileDto user, IEnumerable<LookupItemDto> workshops, bool isEdit)
        {
            InitializeComponent();

            var viewModel = new UserEditDialogViewModel(user, workshops, isEdit);
            viewModel.CloseRequested += result =>
            {
                DialogResult = result;
                Close();
            };

            DataContext = viewModel;
        }
    }
}
