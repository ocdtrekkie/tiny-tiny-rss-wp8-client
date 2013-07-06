using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TinyTinyRSS.Interface.Classes;

namespace TinyTinyRSS.Classes
{
    public class WrappedArticle
    {
        public Headline Headline { get; set; }
        public Article Article { get; set; }

        public WrappedArticle(Headline head)
        {
            this.Headline = head;
        }
    }
}
