using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace QualityControlSystem.WPF.Converters;

public class DoubleToGridLengthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is double width ? new GridLength(width) : new GridLength(0);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is GridLength gridLength ? gridLength.Value : 0d;
    }
}
