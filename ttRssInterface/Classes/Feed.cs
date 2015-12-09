﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;

namespace TinyTinyRSS.Interface.Classes
{
    public sealed class Feed : IComparable<Feed>
    {
        public string title {get;set;}
        public string feed_url {get; set;}
        public int id {get; set;}
        public int unread {get; set;}
        public bool has_icon {get; set;}
        public int cat_id {get; set;}
        public long last_updated {get; set;}
        public int order_id {get; set;}

        public int CompareTo(Feed obj)
        {
            int byUnread = obj.unread.CompareTo(this.unread);
            if (byUnread == 0)
            {
                return this.order_id.CompareTo(obj.order_id);
            }
            else
            {
                return byUnread;
            }
        }

        public string formattedUnread
        {
            get
            {
                string value;
                if (this.unread > 0)
                {
                    value = "(" + unread + ")";
                }
                else
                {
                    value = "(-)";
                }
                return value;
            }
        }

        public string icon
        {
            get
            {
                return InterfaceHelper.getIcon(id);
            }
        }
    }
}
