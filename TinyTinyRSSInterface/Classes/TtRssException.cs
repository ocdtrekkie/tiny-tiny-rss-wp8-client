using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TinyTinyRSS.Interface.Classes
{
    public class TtRssException : SystemException 
    {
       

        public TtRssException(string p, Exception e) : base(p, e)
        {            
        }

        public TtRssException(string p) : base(p)
        {
        }
    }
}
