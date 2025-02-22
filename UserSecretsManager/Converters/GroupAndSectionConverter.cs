using System.Globalization;
using System.Windows.Data;
using System;
using UserSecretsManager.Models;

namespace UserSecretsManager.Converters
{
    public class GroupAndSectionConverter : IMultiValueConverter
    {
        public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2)
            {
                return null;
            }

            if (values[0] is SecretSectionGroupModel group && values[1] is SecretSectionModel selectedSection)
            {
                // Возвращаем Tuple или любую другую структуру
                return (SecretSectionGroup: group, SelectedSecretSection: selectedSection);
            }

            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
