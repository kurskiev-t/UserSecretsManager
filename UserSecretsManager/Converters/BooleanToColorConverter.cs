using System.Globalization;
using System.Windows.Data;
using System;
using System.Windows.Media;

namespace UserSecretsManager.Converters;

public class BooleanToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isSelected && isSelected)
        {
            return Brushes.Black; // Активный вариант — черный
        }
        return Brushes.Gray; // Неактивный — серый
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}