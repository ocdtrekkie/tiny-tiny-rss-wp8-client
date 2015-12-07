using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using TinyTinyRSS.Interface.Classes;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace TinyTinyRSS.Classes
{
    /// <summary>
    /// If author is null or empty, put date in first row.
    /// </summary>
    public class StringToColumnConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string str)
        {
            var strValue = value as String;
            return string.IsNullOrEmpty(strValue) ? 0 : 1;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string str)
        {
            //We can't support this
            throw new NotImplementedException();
        }
    }

    public class IntToStringConverter : IValueConverter
    {
        public object Convert(object value, Type t, object parameter, string str)
        {
            var strValue = value as int?;
            return (strValue == null || strValue.Equals(0)) ? "-" : strValue.ToString();
        }

        public object ConvertBack(object value, Type t, object parameter, string str)
        {
            //We can't support this
            throw new NotImplementedException();
        }
    }
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string str)
        {
            return System.Convert.ToBoolean(value) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string str)
        {
            return value.Equals(Visibility.Visible);
        }
    }
    public class BoolToBoldConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string str)
        {
            return System.Convert.ToBoolean(value) ? Windows.UI.Text.FontWeights.Bold : Windows.UI.Text.FontWeights.Normal;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string str)
        {
            return value.Equals(Windows.UI.Text.FontWeights.Bold);
        }
    }
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string str)
        {
            return value == null ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string str)
        {
            throw new NotImplementedException();
        }
    }
}
