using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TinyTinyRSS.Interface.Classes;

namespace TinyTinyRSS.Classes
{
    public class SpecialFeed : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;        
        private int _count;
        
        public int id {get; set;}
        public string name {get; set;}
        public string icon {get; set;}
        public int count 
        {
            get { return _count; }
            set
            {
                _count = value;
                OnPropertyChanged("count");
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