using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TinyTinyRSS.Interface;
using TinyTinyRSSInterface;
using Windows.Graphics.Imaging;
using Windows.UI.Xaml.Media.Imaging;

namespace TinyTinyRSS.Interface.Classes
{
    public sealed class InterfaceHelper
    {
        public static string getIcon(int? feedId)
        {
            string server = ConnectionSettings.getInstance().server;
            Config conf = TtRssInterface.getInterface().Config;
            if(conf == null || feedId==null) {
                return null;
            }
            
            string iconsUrl = server.Replace("/api/", "/" + conf.icons_url + "/");
            //Uri uri = new Uri(iconsUrl + feedId + ".ico");
            return iconsUrl + feedId + ".ico";
            //return new BitmapImage(uri);
            // see here for http auth https://social.msdn.microsoft.com/Forums/vstudio/en-US/2b24ce07-19d1-46a6-a9b6-d3ac27e57352/image-basic-http-authentication?forum=wpf
        }
    }
}
