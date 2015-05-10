using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TinyTinyRSS.Classes
{
    public class NavigationObject
    {
        public int selectedIndex { get; set; }
        public int feedId { get; set; }
        public Collection<WrappedArticle> ArticlesCollection { get; set; }
        public bool _showUnreadOnly { get; set; }
        public int _sortOrder { get; set; }
    }
}
