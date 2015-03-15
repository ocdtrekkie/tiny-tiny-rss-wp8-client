using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace TinyTinyRSS.Interface.Classes
{
    public sealed class ResponseArray
    {
        public int? seq { get; set; }
        public int status { get; set; }
        public object[] content { get; set; }

        public List<T> getContentAsList<T>() {
            List<object> contentObjects = content.ToList<object>();
            List<T> ret = new List<T>();
            foreach (object single in contentObjects)
            {
                ret.Add(JsonConvert.DeserializeObject<T>(single.ToString()));
            }
            return ret;
        }
        
    }
}
