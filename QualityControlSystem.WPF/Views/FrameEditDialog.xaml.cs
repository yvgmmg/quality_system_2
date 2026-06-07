using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using QualityControlSystem.WPF.Models;

namespace QualityControlSystem.WPF.Views;

public partial class FrameEditDialog : Window
{
    private static readonly string[] SupportedImageExtensions = [".png", ".jpg", ".jpeg"];

    private readonly FrameCardDto _frame;

    public FrameEditDialog(
        FrameCardDto frame,
        IEnumerable<LookupItemDto> materials,
        IEnumerable<LookupItemDto> workshops,
        bool isEdit)
    {
        InitializeComponent();

        _frame = frame;
        Title = isEdit ? "Редактирование каркаса" : "Добавление каркаса";
        TitleText.Text = Title;

        MaterialComboBox.ItemsSource = materials.ToList();
        WorkshopComboBox.ItemsSource = workshops.ToList();

        NameTextBox.Text = _frame.Name;
        MaterialComboBox.SelectedValue = _frame.MaterialTypeId > 0 ? _frame.MaterialTypeId : null;
        WorkshopComboBox.SelectedValue = _frame.WorkshopId > 0 ? _frame.WorkshopId : null;
        WeightTextBox.Text = FormatDecimal(_frame.Weight);
        LengthTextBox.Text = FormatDecimal(_frame.Length);
        WidthTextBox.Text = FormatDecimal(_frame.Width);
        HeightTextBox.Text = FormatDecimal(_frame.Height);
        ImagePathTextBox.Text = _frame.ImagePath;
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Выберите изображение каркаса",
            Filter = "Изображения PNG/JPEG (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog() == true)
            ImagePathTextBox.Text = dialog.FileName;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var name = NameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            ShowValidationMessage("Укажите название каркаса.");
            NameTextBox.Focus();
            return;
        }

        if (MaterialComboBox.SelectedValue is not int materialTypeId)
        {
            ShowValidationMessage("Выберите материал.");
            MaterialComboBox.Focus();
            return;
        }

        if (WorkshopComboBox.SelectedValue is not int workshopId)
        {
            ShowValidationMessage("Выберите цех.");
            WorkshopComboBox.Focus();
            return;
        }

        if (!TryReadDecimal(WeightTextBox.Text, "Вес", out var weight) ||
            !TryReadDecimal(LengthTextBox.Text, "Длина", out var length) ||
            !TryReadDecimal(WidthTextBox.Text, "Ширина", out var width) ||
            !TryReadDecimal(HeightTextBox.Text, "Высота", out var height))
        {
            return;
        }

        var imagePath = ImagePathTextBox.Text.Trim();
        if (!string.IsNullOrWhiteSpace(imagePath) && !IsSupportedImagePath(imagePath))
        {
            ShowValidationMessage("Выберите изображение в формате PNG или JPEG.");
            ImagePathTextBox.Focus();
            return;
        }

        _frame.Name = name;
        _frame.MaterialTypeId = materialTypeId;
        _frame.WorkshopId = workshopId;
        _frame.Weight = weight;
        _frame.Length = length;
        _frame.Width = width;
        _frame.Height = height;
        _frame.ImagePath = string.IsNullOrWhiteSpace(imagePath) ? null : imagePath;

        DialogResult = true;
    }

    private static bool TryReadDecimal(string value, string fieldName, out decimal? result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(value))
            return true;

        var normalized = value.Trim().Replace(',', '.');
        if (decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) && parsed >= 0)
        {
            result = parsed;
            return true;
        }

        ShowValidationMessage($"{fieldName} должен быть положительным числом.");
        return false;
    }

    private static string FormatDecimal(decimal? value)
    {
        return value?.ToString(CultureInfo.CurrentCulture) ?? string.Empty;
    }

    private static bool IsSupportedImagePath(string path)
    {
        var extension = Path.GetExtension(path);
        return SupportedImageExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }

    private static void ShowValidationMessage(string message)
    {
        MessageBox.Show(message, "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}
