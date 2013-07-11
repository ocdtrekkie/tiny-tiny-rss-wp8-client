﻿using Microsoft.Phone.Controls;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace TinyTinyRSS.Classes
{
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
        System.Globalization.CultureInfo culture)
        {
            var strValue = value as String;
            return string.IsNullOrEmpty(strValue) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
        System.Globalization.CultureInfo culture)
        {
            //We can't support this
            throw new NotImplementedException();
        }
    }

    public class IntToStringConverter : IValueConverter
    {
        public object Convert(object value, Type t, object parameter, CultureInfo culture)
        {
            var strValue = value as int?;
            return (strValue == null || strValue.Equals(0)) ? "-" : strValue.ToString();
        }

        public object ConvertBack(object value, Type t, object parameter, CultureInfo culture)
        {
            //We can't support this
            throw new NotImplementedException();
        }
    }
}
