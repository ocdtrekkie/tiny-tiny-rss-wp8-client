using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TinyTinyRSS.Interface.Classes
{
    public class Config
    {
        public string icons_dir {get; set;}
        public string icons_url {get; set;}
        public bool daemon_is_running {get; set;}
        public int num_feeds {get; set;}
    }
}
