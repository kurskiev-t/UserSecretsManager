using System;
using System.Globalization;
using System.Windows.Data;

namespace UserSecretsManager.Converters;

public class StartsWithCommentConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is string rawContent && rawContent.TrimStart().StartsWith("//");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}