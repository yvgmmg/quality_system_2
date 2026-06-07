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

        public bool ShowProductionEquipmentDialog(ProductionEquipmentDto equipment, IEnumerable<LookupItemDto> workshops, bool isEdit)
        {
            var dialog = new ProductionEquipmentEditDialog(equipment, workshops, isEdit)
            {
                Owner = Application.Current.MainWindow
            };

            return dialog.ShowDialog() == true;
        }

        public bool ShowFrameDialog(FrameCardDto frame, IEnumerable<LookupItemDto> materials, IEnumerable<LookupItemDto> workshops, bool isEdit)
        {
            var dialog = new FrameEditDialog(frame, materials, workshops, isEdit)
            {
                Owner = Application.Current.MainWindow
            };

            return dialog.ShowDialog() == true;
        }

        public bool ShowTemplateDialog(TemplateDto template, IEnumerable<string> sides, bool isEdit)
        {
            var dialog = new TemplateEditDialog(template, sides)
            {
                Owner = Application.Current.MainWindow
            };

            return dialog.ShowDialog() == true;
        }

        public bool ShowQualityTestDialog(
            QualityTestDto test,
            IEnumerable<LookupItemDto> frames,
            IEnumerable<TemplateDto> templates,
            bool isEdit)
        {
            var dialog = new QualityTestEditDialog(test, frames, templates, isEdit)
            {
                Owner = Application.Current.MainWindow
            };

            return dialog.ShowDialog() == true;
        }
    }
}
