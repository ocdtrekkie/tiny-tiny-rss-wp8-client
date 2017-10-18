using System.ComponentModel;
using Windows.ApplicationModel.Resources;
using Windows.Graphics.Display;

namespace TinyTinyRSS.Interface.Classes
{
    public sealed class Article : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public int id { get; set; }
        private bool _unread;
        private bool _marked;
        public string title { get; set; }
        public string link { get; set; }
        public object[] labels { get; set; }
        public bool unread
        {
            get { return _unread; }
            set
            {
                _unread = value;
                OnPropertyChanged("unread");
            }
        }
        public bool marked
        {
            get { return _marked; }
            set
            {
                _marked = value;
                OnPropertyChanged("marked");
            }
        }
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
                 _content = "<html><head><meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">"
                     + "<style type=\"text/css\" title=\"text/css\">" + colorStyle +"</style>"
                    + "<link href=\"ms-appx-web:///Strings/style.css\" rel=\"stylesheet\" /></head>"
                    + "<body>" + value
                    + "<br /><a href=\"" + link + "\">" + loader.GetString("LoadOriginalLink")
                    + "</a></body></html>";
            }
        }
        public int? feed_id { get; set; }
        public object attachments { get; set; }
        public int score { get; set; }
        public string feed_title { get; set; }

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
