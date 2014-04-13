using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TinyTinyRSS.Interface;
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

        public async Task<Article> getContent()
        {
            if(Article==null) {
            Article = await TtRssInterface.getInterface().getArticle(Headline.id, false);
            }
            return Article;
        }
    }
}
