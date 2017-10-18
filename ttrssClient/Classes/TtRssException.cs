using System;

namespace TinyTinyRSS.Interface.Classes
{
    public sealed class TtRssException : Exception 
    {
       

        public TtRssException(string p, Exception e) : base(p, e)
        {            
        }

        public TtRssException(string p) : base(p)
        {
        }
    }
}
