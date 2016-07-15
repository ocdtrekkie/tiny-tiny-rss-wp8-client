using System;
using Windows.ApplicationModel.Resources;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
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

    public class ZeroToNoStringConverter : IValueConverter
    {
        public object Convert(object value, Type t, object parameter, string str)
        {
            var strValue = value as int?;
            return (strValue == null || strValue.Equals(0)) ? "" : strValue.ToString();
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

    public class MultiSelectToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string str)
        {
            ListViewSelectionMode listMode = (ListViewSelectionMode) value ;
            return listMode == ListViewSelectionMode.Multiple ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string str)
        {
            throw new NotImplementedException();
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
    public class MarkedSymbolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string str)
        {
            var InteractionMode = UIViewSettings.GetForCurrentView().UserInteractionMode;
            if (InteractionMode == UserInteractionMode.Touch)
            {
                return System.Convert.ToBoolean(value) ? "\uE734" : "\uE735";
            }
            else
            {
                return System.Convert.ToBoolean(value) ? "\uE735" : "\uE734";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string str)
        {
            throw new NotImplementedException();
        }
    }
    public class MarkedTooltipConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string str)
        {
            ResourceLoader loader = new ResourceLoader();
            return System.Convert.ToBoolean(value) ? loader.GetString("UnStarAppBarButtonText") : loader.GetString("StarAppBarButtonText");
        }

        public object ConvertBack(object value, Type targetType, object parameter, string str)
        {
            throw new NotImplementedException();
        }
    }
    public class UnReadSymbolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string str)
        {
            var InteractionMode = UIViewSettings.GetForCurrentView().UserInteractionMode;
            if (InteractionMode == UserInteractionMode.Touch)
            {
                return System.Convert.ToBoolean(value) ? "\uE8C3" : "\uE715";
            }
            else
            {
                return System.Convert.ToBoolean(value) ? "\uE715" : "\uE8C3";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string str)
        {
            throw new NotImplementedException();
        }
    }
    public class UnReadTooltipConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string str)
        {
            ResourceLoader loader = new ResourceLoader();
            return System.Convert.ToBoolean(value) ? loader.GetString("MarkReadAppBarButtonText") : loader.GetString("MarkUnreadAppBarButtonText");
        }

        public object ConvertBack(object value, Type targetType, object parameter, string str)
        {
            throw new NotImplementedException();
        }
    }
    public class TouchToInvisiblityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string str)
        {
            var InteractionMode = UIViewSettings.GetForCurrentView().UserInteractionMode;
            return InteractionMode==UserInteractionMode.Touch ? Visibility.Collapsed: Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string str)
        {
            throw new NotImplementedException();
        }
    }
}
