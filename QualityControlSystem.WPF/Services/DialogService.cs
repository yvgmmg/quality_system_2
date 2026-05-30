using QualityControlSystem.WPF.Services.Interfaces;
using System.Windows;
using QualityControlSystem.WPF.Models;

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
            // Простая заглушка: отображаем сообщение и считаем, что пользователь подтвердил ввод.
            // В реальном приложении здесь будет открыто окно редактирования/добавления пользователя.
            var title = isEdit ? "Редактировать пользователя" : "Новый пользователь";
            var msg = isEdit
                ? $"Редактировать данные пользователя: {user.Surname} {user.Name}?"
                : "Создать нового пользователя";
            MessageBox.Show(msg, title, MessageBoxButton.OK, MessageBoxImage.Information);
            return true; // Предполагаем, что пользователь нажал OK.
        }
    }
}