using System.ComponentModel;

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
        
        public SpecialFeed(string newname, string newicon, int newid) {
            id = newid;
            name = newname;
            icon = newicon;
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