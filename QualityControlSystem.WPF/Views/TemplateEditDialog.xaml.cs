using QualityControlSystem.WPF.Models;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace QualityControlSystem.WPF.Views;

public partial class TemplateEditDialog : Window
{
    private readonly TemplateDto _template;

    public TemplateEditDialog(TemplateDto template, IEnumerable<string> sides)
    {
        InitializeComponent();
        _template = template;

        SideComboBox.ItemsSource = sides.ToList();
        NameTextBox.Text = template.Name;
        SideComboBox.SelectedItem = template.Side;
        ImagePathTextBox.Text = template.ImagePath;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
        {
            MessageBox.Show("Укажите имя шаблона.", "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
            NameTextBox.Focus();
            return;
        }

        if (SideComboBox.SelectedItem is not string side)
        {
            MessageBox.Show("Выберите сторону каркаса.", "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
            SideComboBox.Focus();
            return;
        }

        _template.Name = NameTextBox.Text.Trim();
        _template.Side = side;
        DialogResult = true;
    }
}
