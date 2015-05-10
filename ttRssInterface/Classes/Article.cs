using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.ApplicationModel.Resources;

namespace TinyTinyRSS.Interface.Classes
{
    public sealed class Article
    {
        public int id {get; set;}
        public string title {get; set;}
        public string link {get; set;}
        public object[] labels {get; set;}
        public bool unread {get; set;}
        public bool marked {get; set;}
        public bool published {get; set;}
        public string comments {get; set;}
        public string author {get; set;}
        public long updated {get; set;}
        private string _content;
        public string content {
            get 
            {
                return _content;
            }
            set
            {
                ResourceLoader loader = new Windows.ApplicationModel.Resources.ResourceLoader();
                _content = "<html><body>" + value +
                    "<br /><a href=\"" + link +
                    "\">" +
                    loader.GetString("LoadOriginalLink") +
                    "</a></body></html>";
            }
        }
        public int? feed_id {get; set;}
        public object attachments {get; set;}
        public int score {get; set;}
        public string feed_title { get; set; }
    }
}
