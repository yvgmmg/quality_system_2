using System.Globalization;
using System.Windows;
using QualityControlSystem.Infrastructure.Enums;
using QualityControlSystem.WPF.Models;

namespace QualityControlSystem.WPF.Views;

public partial class RegulatoryInformationEditDialog : Window
{
    private readonly RegulatoryInformationDto _item;

    public RegulatoryInformationEditDialog(RegulatoryInformationDto item, bool isEdit)
    {
        InitializeComponent();

        _item = item;
        Title = isEdit ? "Редактирование НСИ" : "Добавление НСИ";
        TitleText.Text = Title;

        MeasurementComboBox.ItemsSource = Enum.GetValues(typeof(MeasurementUnit))
            .Cast<MeasurementUnit>()
            .Select(value => new MeasurementOption(GetMeasurementDisplay(value), value))
            .ToList();

        NameTextBox.Text = _item.Name;
        TypeComboBox.SelectedValue = string.IsNullOrWhiteSpace(_item.Type) ? "equipment" : _item.Type;
        MinValueTextBox.Text = _item.MinValue?.ToString(CultureInfo.CurrentCulture);
        MaxValueTextBox.Text = _item.MaxValue?.ToString(CultureInfo.CurrentCulture);
        MeasurementComboBox.SelectedValue = _item.Measurement;
        StartDatePicker.SelectedDate = _item.StartDate == default ? DateTime.Today : _item.StartDate;
        EndDatePicker.SelectedDate = _item.EndDate;
        DescriptionTextBox.Text = _item.Description;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
        {
            MessageBox.Show("Название обязательно.", "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (TypeComboBox.SelectedValue is not string type)
        {
            MessageBox.Show("Выберите тип / источник.", "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (MeasurementComboBox.SelectedValue is not MeasurementUnit measurement)
        {
            MessageBox.Show("Выберите единицу измерения.", "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!TryReadNullableDouble(MinValueTextBox.Text, out var minValue) ||
            !TryReadNullableDouble(MaxValueTextBox.Text, out var maxValue))
        {
            MessageBox.Show("Минимальное и максимальное значения должны быть числами.", "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (minValue.HasValue && maxValue.HasValue && minValue.Value > maxValue.Value)
        {
            MessageBox.Show("Минимальное значение не должно быть больше максимального.", "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (StartDatePicker.SelectedDate == null)
        {
            MessageBox.Show("Дата начала обязательна.", "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (EndDatePicker.SelectedDate.HasValue && EndDatePicker.SelectedDate.Value < StartDatePicker.SelectedDate.Value)
        {
            MessageBox.Show("Дата окончания не должна быть раньше даты начала.", "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _item.Name = NameTextBox.Text.Trim();
        _item.Type = type;
        _item.MinValue = minValue;
        _item.MaxValue = maxValue;
        _item.Measurement = measurement;
        _item.StartDate = StartDatePicker.SelectedDate.Value.Date;
        _item.EndDate = EndDatePicker.SelectedDate?.Date;
        _item.Description = string.IsNullOrWhiteSpace(DescriptionTextBox.Text) ? null : DescriptionTextBox.Text.Trim();

        DialogResult = true;
    }

    private static bool TryReadNullableDouble(string text, out double? value)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(text))
            return true;

        if (double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out var parsed) ||
            double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
        {
            value = parsed;
            return true;
        }

        return false;
    }

    private static string GetMeasurementDisplay(MeasurementUnit measurement)
    {
        return measurement.ToString() switch
        {
            "РјРј" => "мм",
            "СЃРј" => "см",
            "Рј" => "м",
            "Рі" => "г",
            "РєРі" => "кг",
            "С‚" => "т",
            "GradC" => "°C",
            "Grad" => "°",
            "Empty" => "%",
            "С€С‚" => "шт",
            "Рќ" => "Н",
            "РњРџР°" => "МПа",
            "Р’" => "В",
            "Рђ" => "А",
            "m_s" => "м/с",
            "m_kv" => "м²",
            "m_kub" => "м³",
            "bezrazm" => "безразм.",
            var value => value
        };
    }

    private sealed record MeasurementOption(string DisplayName, MeasurementUnit Value);
}
