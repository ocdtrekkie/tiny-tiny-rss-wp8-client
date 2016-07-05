using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace TinyTinyRSS.Interface.Classes
{
    public sealed class Category : IComparable<Category>, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public int id {get;set;}
        public string title {get;set; }
        private int _unread;
        public int unread
        {
            get { return _unread; }
            set
            {
                _unread = value;
                OnPropertyChanged("unread");
                OnPropertyChanged("combined");
            }
        }
        public int order_id { get; set; }

        public int CompareTo(Category obj)
        {
            return this.order_id.CompareTo(obj.order_id);
        }

        public string combined
        {
            get
            {
                if (unread == 0)
                {
                    return "(-)";
                }
                else
                {
                    return "(" + unread + ")";
                }
            }
        }

        // Create the OnPropertyChanged method to raise the event 
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
