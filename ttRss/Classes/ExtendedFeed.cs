using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TinyTinyRSS.Interface.Classes;

namespace TinyTinyRSS.Classes
{
    public class ExtendedFeed
    {
        public Category cat {get; set;}
        public Feed feed { get; set; }

        public ExtendedFeed(Feed feed, Category category)
        {
            this.feed = feed;
            this.cat = category;
        }
    }
}
