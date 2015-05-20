using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.ApplicationModel.Resources;
using Windows.Graphics.Display;
using Windows.UI.ViewManagement;

namespace TinyTinyRSS.Interface.Classes
{
    public sealed class Article
    {
        public int id { get; set; }
        public string title { get; set; }
        public string link { get; set; }
        public object[] labels { get; set; }
        public bool unread { get; set; }
        public bool marked { get; set; }
        public bool published { get; set; }
        public string comments { get; set; }
        public string author { get; set; }
        public long updated { get; set; }
        private string _content;
        public string content
        {
            get
            {
                return _content;
            }
            set
            {
                ResourceLoader loader = new Windows.ApplicationModel.Resources.ResourceLoader();
                ResolutionScale resolutionScale = DisplayInformation.GetForCurrentView().ResolutionScale;
                double factor = (double)resolutionScale / 100.0;
                double maxWidth = 340 * factor;
                string colorStyle = "";
                if (ConnectionSettings.getInstance().useDarkBackground)
                {
                    colorStyle = "html, body { color: #fff!Important; background-color: #000;} " +
                        "a:link {color: #81DAF5;} ";
                }
                _content = "<html><head><meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\"><style type=\"text/css\" title=\"text/css\">"
                    + colorStyle +
                    "img, iframe{width:100%;max-width:" + maxWidth.ToString() + "px;height:auto;}</style>" + "</head>"
                    + "<body>" + value +
                    "<br /><a href=\"" + link +
                    "\">" +
                    loader.GetString("LoadOriginalLink") +
                    "</a></body></html>";
            }
        }
        public int? feed_id { get; set; }
        public object attachments { get; set; }
        public int score { get; set; }
        public string feed_title { get; set; }
    }
}
