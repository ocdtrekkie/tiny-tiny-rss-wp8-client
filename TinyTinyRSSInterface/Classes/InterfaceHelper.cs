using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using TinyTinyRSS.Interface;
using TinyTinyRSSInterface;

namespace TinyTinyRSS.Interface.Classes
{
    public class InterfaceHelper
    {
        public static BitmapImage getIcon(int feedId)
        {
            string server = ConnectionSettings.getInstance().server;
            Config conf = TtRssInterface.getInterface().Config;
            if(conf == null) {
                return null;
            }
            string iconsUrl = server.Replace("/api/", "/" + conf.icons_url + "/");
            Uri uri = new Uri(iconsUrl + feedId + ".ico");
            return new BitmapImage(uri);
        }
    }
}
