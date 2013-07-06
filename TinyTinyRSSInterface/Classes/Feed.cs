using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TinyTinyRSS.Interface.Classes
{
    public class Feed
    {
        public string title {get;set;}
        public string feed_url {get; set;}
        public int id {get; set;}
        public int unread {get; set;}
        public bool has_icon {get; set;}
        public int cat_id {get; set;}
        public long last_updated {get; set;}
        public int order_id {get; set;}
    }
}
