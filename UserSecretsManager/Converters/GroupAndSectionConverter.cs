using System.Globalization;
using System.Windows.Data;
using System;
using UserSecretsManager.Models;

namespace UserSecretsManager.Converters
{
    public class GroupAndSectionConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2)
            {
                var group = values[0] as SecretSectionGroupModel;
                var selectedSection = values[1] as SecretSectionModel;

                if (group != null && selectedSection != null)
                {
                    // Возвращаем Tuple или любую другую структуру
                    return (SecretSectionGroup: group, SelectedSecretSection: selectedSection);
                }
            }

            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
