﻿using System.Globalization;
using System.Windows.Data;
using System;

namespace UserSecretsManager.Converters;

public class StringNullOrEmptyToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        !string.IsNullOrEmpty(value as string);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}