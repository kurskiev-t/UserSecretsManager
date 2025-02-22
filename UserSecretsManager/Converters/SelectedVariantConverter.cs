using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.Windows.Data;
using UserSecretsManager.Models;

namespace UserSecretsManager.Converters
{
    //public class SelectedVariantConverter : IMultiValueConverter
    //{
    //    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        if (values.Length != 2 || values[0] is not SecretSectionModel currentlySelectedSectionInUi || values[1] is not SecretSectionModel selectedSectionVariantInGroup)
    //        {
    //            return false;
    //        }
    //        return currentlySelectedSectionInUi == selectedSectionVariantInGroup;
    //    }

    //    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    //    {
    //        if (value is bool isChecked && isChecked && targetTypes.Length == 2)
    //        {
    //            return new object[] { targetTypes[0], null }; // Возвращаем текущую модель для обновления
    //        }
    //        return new object[] { Binding.DoNothing, Binding.DoNothing };
    //    }
    //}

    public class SelectedVariantConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2 || values[0] is not SecretSectionModel currentlySelectedSectionInUi || values[1] is not SecretSectionModel selectedSectionVariantInGroup)
            {
                return false;
            }
            return currentlySelectedSectionInUi == selectedSectionVariantInGroup;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            if (value is bool isChecked && isChecked && parameter is SecretSectionModel currentlySelectedSectionInUi)
            {
                // Возвращаем текущую модель как новое значение для SelectedVariant
                return new object[] { currentlySelectedSectionInUi, currentlySelectedSectionInUi };
            }
            return new object[] { Binding.DoNothing, Binding.DoNothing };
        }
    }
}
