using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using TinyTinyRSS.Interface;
using TinyTinyRSS.Interface.Classes;
using Windows.ApplicationModel.Resources;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Imaging;

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
    public class FeedIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string str)
        {
            string feedId = value.ToString();
            string server = ConnectionSettings.getInstance().server;
            Config conf = TtRssInterface.getInterface().Config;
            if (conf == null || feedId == null)
            {
                return null;
            }

            string iconsUrl = server.Replace("/api/", "/" + conf.icons_url + "/") + feedId + ".ico";
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(iconsUrl);
                if (ConnectionSettings.getInstance().httpAuth)
                {
                    request.Credentials = new NetworkCredential(
                        ConnectionSettings.getInstance().username, 
                        ConnectionSettings.getInstance().password);
                } else
                {
                    return iconsUrl;
                }

                Task<WebResponse> response = request.GetResponseAsync();
                Task.WaitAll(response);
                WebResponse resp = response.Result;
                BitmapImage image = new BitmapImage();
                using (var responseStream = resp.GetResponseStream()) { 
                    using (var memStream = new MemoryStream()) 
                    {
                        Task memStreamTask = responseStream.CopyToAsync(memStream);
                        Task.WaitAll(memStreamTask);
                        memStream.Position = 0;
                        image.SetSource(memStream.AsRandomAccessStream());
                    }
                }
                return image;
            }
            catch (Exception)
            {
                return "";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string str)
        {
            throw new NotImplementedException();
        }
    }
}

