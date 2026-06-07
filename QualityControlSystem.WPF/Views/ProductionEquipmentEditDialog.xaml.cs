using System.Collections.Generic;
using System.Linq;
using System.Windows;
using QualityControlSystem.WPF.Models;

namespace QualityControlSystem.WPF.Views;

public partial class ProductionEquipmentEditDialog : Window
{
    private readonly ProductionEquipmentDto _equipment;

    public ProductionEquipmentEditDialog(
        ProductionEquipmentDto equipment,
        IEnumerable<LookupItemDto> workshops,
        bool isEdit)
    {
        InitializeComponent();

        _equipment = equipment;
        Title = isEdit ? "Редактирование оборудования" : "Добавление оборудования";
        TitleText.Text = Title;

        WorkshopComboBox.ItemsSource = workshops.ToList();

        NameTextBox.Text = _equipment.Name;
        SerialNumberTextBox.Text = _equipment.SerialNumber;
        OkofCodeTextBox.Text = _equipment.OkofCode;
        InventoryNumberTextBox.Text = _equipment.InventoryNumber;
        WorkshopComboBox.SelectedValue = _equipment.WorkshopId > 0 ? _equipment.WorkshopId : null;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var name = NameTextBox.Text.Trim();
        var serialNumber = SerialNumberTextBox.Text.Trim();
        var okofCode = OkofCodeTextBox.Text.Trim();
        var inventoryNumber = InventoryNumberTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            ShowValidationMessage("Укажите название оборудования.");
            NameTextBox.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(okofCode))
        {
            ShowValidationMessage("Укажите код ОКОФ.");
            OkofCodeTextBox.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(inventoryNumber))
        {
            ShowValidationMessage("Укажите инвентарный номер.");
            InventoryNumberTextBox.Focus();
            return;
        }

        if (WorkshopComboBox.SelectedValue is not int workshopId)
        {
            ShowValidationMessage("Выберите цех.");
            WorkshopComboBox.Focus();
            return;
        }

        if (name.Length > 255 || serialNumber.Length > 20 || okofCode.Length > 19 || inventoryNumber.Length > 17)
        {
            ShowValidationMessage("Проверьте длину полей: название до 255, серийный номер до 20, ОКОФ до 19, инвентарный номер до 17 символов.");
            return;
        }

        _equipment.Name = name;
        _equipment.SerialNumber = string.IsNullOrWhiteSpace(serialNumber) ? null : serialNumber;
        _equipment.OkofCode = okofCode;
        _equipment.InventoryNumber = inventoryNumber;
        _equipment.WorkshopId = workshopId;

        DialogResult = true;
    }

    private static void ShowValidationMessage(string message)
    {
        MessageBox.Show(message, "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}
