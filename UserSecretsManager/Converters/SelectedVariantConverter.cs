using System;
using System.Globalization;
using System.Windows.Data;
using UserSecretsManager.Models;

namespace UserSecretsManager.Converters
{
    public class SelectedVariantConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Если это выбранный вариант, возвращаем true, иначе false
            var selectedVariant = value as SecretSectionModel;
            var currentVariant = parameter as SecretSectionModel;

            return selectedVariant == currentVariant;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Если value == true, это значит, что нужно выбрать этот вариант
            return value is bool isSelected && isSelected ? parameter : Binding.DoNothing;
        }
    }
}
