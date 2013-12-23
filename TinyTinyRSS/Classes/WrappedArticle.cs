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

        public override bool Equals(object obj)
        {
            if ((obj != null) && (obj.GetType() == typeof(Microsoft.Phone.Controls.PanoramaItem)))
            {
                Microsoft.Phone.Controls.PanoramaItem thePanoItem = (Microsoft.Phone.Controls.PanoramaItem)obj;

                return base.Equals(thePanoItem.Header);
            }
            else
            {
                return base.Equals(obj);
            }
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
