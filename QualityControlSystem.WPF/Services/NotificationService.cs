using QualityControlSystem.WPF.Services.Interfaces;
using System.Windows;

namespace QualityControlSystem.WPF.Services
{
    public class NotificationService : INotificationService
    {
        public void ShowSuccess(string message)
        {
            MessageBox.Show(message, "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void ShowError(string message)
        {
            MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public void ShowWarning(string message)
        {
            MessageBox.Show(message, "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}