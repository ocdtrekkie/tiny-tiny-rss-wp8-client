using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace TinyTinyRSS.Interface.Classes
{
    public class Response
    {
        public int? seq { get; set; }
        public int status { get; set; }
        public object content { get; set; }

        public T getContent<T>() {
            return JsonConvert.DeserializeObject<T>(content.ToString());
        }
    }
}
