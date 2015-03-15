using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TinyTinyRSS.Interface.Classes
{
    public sealed class Category : IComparable<Category> 
    {
        public int id {get;set;}
        public string title {get;set;}
        public int unread {get;set;}
        public int order_id { get; set; }

        public int CompareTo(Category obj)
        {
            return this.title.CompareTo(obj.title);
        }

        public string combined
        {
            get
            {
                if (unread == 0)
                {
                    return "(-)";
                }
                else
                {
                    return "(" + unread + ")";
                }
            }
        }
    }
}
