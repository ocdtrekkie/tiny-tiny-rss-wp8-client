using System;
using System.ComponentModel;

namespace TinyTinyRSS.Interface.Classes
{
    public sealed class Feed : IComparable<Feed>,INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public string title {get;set;}
        public string feed_url {get; set;}
        public int id {get; set; }
        private int _unread;
        public int unread
        {
            get { return _unread; }
            set
            {
                _unread = value;
                OnPropertyChanged("unread");
            }
        }
        public bool has_icon {get; set;}
        public int cat_id {get; set;}
        public long last_updated {get; set;}
        public int order_id {get; set;}

        public int CompareTo(Feed obj)
        {
            return this.order_id.CompareTo(obj.order_id);
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

        // Create the OnPropertyChanged method to raise the event 
        public void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
