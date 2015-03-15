using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TinyTinyRSSInterface.Classes
{
    public enum UpdateField
    {
        Starred = 0,
        Published = 1,
        Unread = 2,
        Archived = 4
    }

    public enum UpdateMode 
    {
        False = 0,
        True = 1,
        Toggle = 2
    }
}
