using System.Windows;
using QualityControlSystem.WPF.Models;
using QualityControlSystem.WPF.Services.Interfaces;
using QualityControlSystem.WPF.Views;

namespace QualityControlSystem.WPF.Services
{
    public class DialogService : IDialogService
    {
        public void ShowMessage(string message, string title = "Информация")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public bool ShowConfirm(string message, string title = "Подтверждение")
        {
            var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
            return result == MessageBoxResult.Yes;
        }

        public bool ShowUserDialog(UserProfileDto user, bool isEdit)
        {
            var dialog = new UserEditDialog(user, isEdit)
            {
                Owner = Application.Current.MainWindow
            };

            return dialog.ShowDialog() == true;
        }

        public bool ShowRegulatoryInformationDialog(RegulatoryInformationDto item, bool isEdit)
        {
            var dialog = new RegulatoryInformationEditDialog(item, isEdit)
            {
                Owner = Application.Current.MainWindow
            };

            return dialog.ShowDialog() == true;
        }
    }
}
