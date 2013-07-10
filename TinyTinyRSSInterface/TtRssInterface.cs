using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using TinyTinyRSS.Interface.Classes;
using TinyTinyRSSInterface;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows;
using TinyTinyRSSInterface.Classes;

namespace TinyTinyRSS.Interface
{
    public class TtRssInterface  {

        private static TtRssInterface instance;
        private string sessionId;
        private static string SidPlaceholder = "StringToReplaceBySessionId";

        private TtRssInterface() {
        }

        public static TtRssInterface getInterface()
        {
            if (instance == null)
            {
                instance = new TtRssInterface();
            }
            return instance;
        }

        public async Task Login()
        {
            string login = "{\"op\":\"login\",\"user\":\"" + ConnectionSettings.getInstance().username + "\",\"password\":\"" + ConnectionSettings.getInstance().password + "\"}";
            Response response = await SendRequestAsync(null, login);
            try
            {
                Session session = ParseContentOrError<Session>(response);
                this.sessionId = session.session_id;
            }
            catch (TtRssException)
            {
                // Popup mit errormessage.
            }
        }

        public async Task<string> CheckLogin(string server, string username, string password)
        {
            string login = "{\"op\":\"login\",\"user\":\"" + username + "\",\"password\":\"" + password + "\"}";
            try
            {
                Response response = await SendRequestAsync(server, login);
                Session session = ParseContentOrError<Session>(response);
                return "";
            }
            catch (TtRssException e)
            {
                if (e.Message.Equals("Unexpected character encountered while parsing value: <. Path '', line 0, position 0."))
                {
                    return string.Concat("Something went wrong, probably your Server Url is misspelled.", e.Message);
                }
                return e.Message;
            }
            
        }

        public async Task<bool> CheckLogin()
        {
            string login = "{\"op\":\"login\",\"user\":\"" + ConnectionSettings.getInstance().username + "\",\"password\":\"" + ConnectionSettings.getInstance().password + "\"}";
            try
            {
                Response response = await SendRequestAsync(null, login);
                Session session = ParseContentOrError<Session>(response);
                return true;
            }
            catch (TtRssException)
            {
                return false;
            }

        }

        public async Task<int> getUnReadCount() {
            string unreadReq = "{\"sid\":\"" + SidPlaceholder + "\",\"op\":\"getUnread\"}";
            Response unreadResp = await SendRequestAsync(null, unreadReq);
            UnreadCount unread = ParseContentOrError<UnreadCount>(unreadResp);
            return unread.unread;
        }

        public async Task<List<Headline>> getHeadlines(int id)
        {
            string getHeadlines = "{\"sid\":\"" + SidPlaceholder + "\",\"op\":\"getHeadlines\",\"show_excerpt\":false,\"limit\":50, \"feed_id\":" + (int)id + "}";
            Console.WriteLine(getHeadlines);
            ResponseArray unreadItems = await SendRequestArrayAsync(null, getHeadlines);
            List<Headline> headlines = ParseContentOrError<Headline>(unreadItems);
            Console.WriteLine(headlines.Count);
            return headlines;
        }

        public async Task<Article> getArticle(int id)
        {
            string getArticle = "{\"sid\":\"" + SidPlaceholder + "\",\"op\":\"getArticle\",\"article_id\":" + id + "}";
            ResponseArray articleResp = await SendRequestArrayAsync(null, getArticle);
            Article article = ParseContentOrError<Article>(articleResp)[0];
            return article;
        }

        public async Task<bool> updateArticle(int id, UpdateField field, UpdateMode mode)
        {
            string updateOp = "{\"sid\":\"" + SidPlaceholder + "\",\"op\":\"updateArticle\",\"article_ids\":" + id + ",\"mode\":"+ (int) mode +",\"field\":"+ (int) field +"}";
            Response response = await SendRequestAsync(null, updateOp);
            if (response.content.ToString().Contains("OK"))
            {
                return true;
            }
            return false;
        }

        public async Task<List<Feed>> getFeeds()
        {
            string getFeeds = "{\"sid\":\"" + SidPlaceholder + "\",\"op\":\"getFeeds\",\"cat_id\":-3,\"unread_only\":false}";
            Console.WriteLine(getFeeds);
            ResponseArray response = await SendRequestArrayAsync(null, getFeeds);
            List<Feed> feeds = ParseContentOrError<Feed>(response);
            Console.WriteLine(feeds.Count);
            return feeds;
        }

        public async Task<List<Category>> getCategories()
        {
            string getCategories = "{\"sid\":\"" + SidPlaceholder + "\",\"op\":\"getCategories\",\"unread_only\":false,\"enable_nested\":false,\"include_empty\":true}";
            Console.WriteLine(getCategories);
            ResponseArray response = await SendRequestArrayAsync(null, getCategories);
            List<Category> cats = ParseContentOrError<Category>(response);
            Console.WriteLine(cats.Count);
            return cats;
        }

        public async Task<Response> SendRequestAsync(string server, string requestUrl)
        {
            if (sessionId == null && !requestUrl.Contains("\"op\":\"login\""))
            {
                await Login();
            }
            requestUrl = requestUrl.Replace(SidPlaceholder, sessionId);

            Console.WriteLine(requestUrl);
            try
            {
                if (server == null)
                {
                    server = ConnectionSettings.getInstance().server;
                }
                HttpWebRequest request = (HttpWebRequest) WebRequest.Create(server);
                byte[] postBytes = Encoding.UTF8.GetBytes(requestUrl);
                request.Method = HttpMethod.Post;
                request.ContentType = "application/json; charset=UTF-8";
                request.Accept = "application/json";
                request.ContentLength = postBytes.Length;

                Stream requestStream = await request.GetRequestStreamAsync();
                requestStream.Write(postBytes, 0, postBytes.Length);
                requestStream.Close();

                HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();
                using (var sr = new StreamReader(response.GetResponseStream()))
                {
                    string responseString = sr.ReadToEnd();
                    Console.WriteLine(responseString);
                    return JsonConvert.DeserializeObject<Response>(responseString);
                }
            }
            catch (Exception e)
            {
                throw new TtRssException(e.Message, e);
            }
        }

        public async Task<ResponseArray> SendRequestArrayAsync(string server, string requestUrl)
        {
            if (sessionId == null && !requestUrl.Contains("\"op\":\"login\""))
            {
                await Login();
            }
            requestUrl = requestUrl.Replace(SidPlaceholder, sessionId);      
     
            Console.WriteLine(requestUrl);
            if (server == null)
            {
                server = ConnectionSettings.getInstance().server;
            }
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(server);
            byte[] postBytes = Encoding.UTF8.GetBytes(requestUrl);
            request.Method = HttpMethod.Post;
            request.ContentType = "application/json; charset=UTF-8";
            request.Accept = "application/json";
            request.ContentLength = postBytes.Length;

            Stream requestStream = await request.GetRequestStreamAsync();
            requestStream.Write(postBytes, 0, postBytes.Length);
            requestStream.Close();
            HttpWebResponse httpResponse = (HttpWebResponse) await request.GetResponseAsync();
            using (var sr = new StreamReader(httpResponse.GetResponseStream()))
            {
                string responseString = sr.ReadToEnd();
                Console.WriteLine(responseString);
                try
                {
                    return JsonConvert.DeserializeObject<ResponseArray>(responseString);
                }
                catch (JsonSerializationException e)
                {
                    Response response = JsonConvert.DeserializeObject<Response>(responseString);
                    Error error = response.getContent<Error>();
                    Console.WriteLine("errorMessage: " + error.error);
                    throw new TtRssException("Error occured: " + error.error, e);
                }
            }   
        }

        public T ParseContentOrError<T>(Response response) {
            Console.WriteLine("status: " + response.status);
            Console.WriteLine("seq: " + response.seq);
            string contentString = response.content.ToString();
            Console.WriteLine("content: " + contentString);            
            if (response.status == 1)
            {
                Error error = response.getContent<Error>();
                Console.WriteLine("errorMessage: " + error.error);
                throw new TtRssException("Error occured: " + error.error);
            }
            else
            {
                return response.getContent<T>();
            }
        }

        public List<T> ParseContentOrError<T>(ResponseArray response)
        {
            Console.WriteLine("status: " + response.status);
            Console.WriteLine("seq: " + response.seq);
            string contentString = response.content.ToString();
            Console.WriteLine("content: " + contentString);
            if (response.status == 1)
            {
                Error error = JsonConvert.DeserializeObject<Error>(contentString);
                Console.WriteLine("errorMessage: " + error.error);
                throw new TtRssException("Error occured: " + error.error);
            }
            else
            {
                return response.getContentAsList<T>();
            }
        }
    }
}
