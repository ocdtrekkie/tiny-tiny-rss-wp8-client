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
using CaledosLab.Portable.Logging;

namespace TinyTinyRSS.Interface
{
    public class TtRssInterface
    {
        public const int INITIALHEADLINECOUNT = 20;
        public const int ADDITIONALHEADLINECOUNT = 5;
        public const string NONETWORKERROR = "HTTP Response is null.";

        private static TtRssInterface instance;
        private string sessionId;
        private Dictionary<int, Feed> FeedDictionary;
        private LimitedSizeDictionary<int, Article> ArticleCache;
        private Dictionary<int, int> GlobalCounter;
        private Dictionary<int, int> FeedCounter;
        private Dictionary<int, int> CategoryCounter;
        public Config Config { get; set; }
        private static string SidPlaceholder = "StringToReplaceBySessionId";

        private TtRssInterface()
        {
            FeedDictionary = new Dictionary<int, Feed>();
            ArticleCache = new LimitedSizeDictionary<int, Article>(20);
            GlobalCounter = new Dictionary<int, int>();
            FeedCounter = new Dictionary<int, int>();
            CategoryCounter = new Dictionary<int, int>();
        }

        public static TtRssInterface getInterface()
        {
            if (instance == null)
            {
                instance = new TtRssInterface();
            }
            return instance;
        }

        public async Task Login(bool renewSession)
        {
            if (sessionId == null || renewSession)
            {
                try
                {
                    string login = "{\"op\":\"login\",\"user\":\"" + ConnectionSettings.getInstance().username + "\",\"password\":\"" + ConnectionSettings.getInstance().password + "\"}";
                    Response response = await SendRequestAsync(null, login);
                    Session session = ParseContentOrError<Session>(response);
                    this.sessionId = session.session_id;
                    Config = await getConfig(true);
                }
                catch (TtRssException e)
                {
                    throw e;
                }
            }
        }

        public async Task<string> CheckLogin(string server, string username, string password)
        {
            string login = "{\"op\":\"login\",\"user\":\"" + username + "\",\"password\":\"" + password + "\"}";
            try
            {
                Response response = await SendRequestAsync(server, login);
                Session session = ParseContentOrError<Session>(response);
                sessionId = session.session_id;
                //Config = await getConfig(false);
                return "";
            }
            catch (TtRssException e)
            {
                if (e.Message.Equals("Unexpected character encountered while parsing value: <. Path '', line 0, position 0."))
                {
                    return string.Concat("Something went wrong, probably your Server Url is misspelled.", e.Message);
                }
                else if (e.Message.Equals("Error occured: JSON Deserialization returned null.") || e.Message.Equals(NONETWORKERROR))
                {
                    return "Something went wrong. May you're not connected to the web.";
                }
                return e.Message;
            }
        }

        public async Task<bool> CheckLogin()
        {
            if (sessionId != null)
            {
                return true;
            }
            else
            {
                string login = "{\"op\":\"login\",\"user\":\"" + ConnectionSettings.getInstance().username + "\",\"password\":\"" + ConnectionSettings.getInstance().password + "\"}";
                try
                {
                    Response response = await SendRequestAsync(null, login);
                    Session session = ParseContentOrError<Session>(response);
                    sessionId = session.session_id;
                    Config = await getConfig(false);
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public async Task<int> getUnReadCount()
        {
            try
            {
                string unreadReq = "{\"sid\":\"" + SidPlaceholder + "\",\"op\":\"getUnread\"}";
                Response unreadResp = await SendRequestAsync(null, unreadReq);
                UnreadCount unread = ParseContentOrError<UnreadCount>(unreadResp);
                return unread.unread;
                //return GlobalCounter[0]; this one only reads "new" unread articles.
            }
            catch (TtRssException e)
            {
                throw e;
            }
        }

        public async Task<int> getCounters()
        {
            try
            {
                string request = "{\"sid\":\"" + SidPlaceholder + "\",\"op\":\"getCounters\"}";
                ResponseArray response = await SendRequestArrayAsync(null, request);
                List<Counter> counters = ParseContentOrError<Counter>(response);
                GlobalCounter.Clear();
                CategoryCounter.Clear();
                FeedCounter.Clear();
                foreach (Counter c in counters)
                {
                    int parsedId;
                    if (c.id.Equals("global-unread"))
                    {
                        GlobalCounter.Add(0, await getUnReadCount()); // The one of getCounters does not count "old" articles
                    }
                    else if (c.id.Equals("subscribed-feeds"))
                    {
                        GlobalCounter.Add(1, c.counter);
                    }
                    else if (int.TryParse(c.id, out parsedId))
                    {
                        if (c.kind != null && c.kind.Equals("cat"))
                        {
                            CategoryCounter.Add(parsedId, c.counter);
                        }
                        else
                        {
                            if (parsedId <= 0 && parsedId > -3)
                            {
                                FeedCounter.Add(parsedId, int.Parse(c.auxcounter));
                            }
                            else
                            {
                                FeedCounter.Add(parsedId, c.counter);
                            }
                        }
                    }
                }
                return counters.Count;
            }
            catch (TtRssException e)
            {
                throw e;
            }
        }

        public async Task<int> getCountForFeed(bool forceUpdate, int feedId)
        {
            if (feedId == (int)FeedId.Fresh)
            {
                feedId = (int) FeedId.All;
            }
            if (forceUpdate || !FeedCounter.ContainsKey(feedId))
            {
                await getCounters();
            }
            return FeedCounter[feedId];
        }

        public async Task<int> getCountForCategory(bool forceUpdate, int feedId)
        {
            if (forceUpdate || !CategoryCounter.ContainsKey(feedId))
            {
                await getCounters();
            }
            return CategoryCounter[feedId];
        }

        public async Task<List<Headline>> getHeadlines(int id, bool unreadOnly, int skip, int sortOrder)
        {
            string view_mode = "all_articles";
            int limit = INITIALHEADLINECOUNT;
            string sort;
            if (unreadOnly)
                view_mode = "unread";
            switch (sortOrder)
            {
                case 1: 
                    sort = "feed_dates";
                    break;
                case 2: 
                    sort = "date_reverse";
                    break;
                default:
                    sort = "";
                    break;
            }
            if (skip > 0)
            {
                limit = ADDITIONALHEADLINECOUNT;
            }
            try
            {
                string getHeadlines = "{\"sid\":\"" + SidPlaceholder + "\",\"op\":\"getHeadlines\",\"show_excerpt\":false,\"limit\":" + limit + ",\"skip\":" + skip + ", \"view_mode\":\"" + view_mode + "\", \"feed_id\":" + (int)id + ", \"order_by\":\"" + sort + "\"}";
                ResponseArray unreadItems = await SendRequestArrayAsync(null, getHeadlines);
                List<Headline> headlines = ParseContentOrError<Headline>(unreadItems);
                return headlines;
            }
            catch (TtRssException e)
            {
                throw e;
            }
        }

        public async Task<Article> getArticle(int id, bool forceRefresh)
        {
            try
            {
                if (!ArticleCache.ContainsKey(id) || forceRefresh)
                {
                    if (!forceRefresh)
                        Logger.WriteLine("ARTICLE not in Cache: " + id);

                    string getArticle = "{\"sid\":\"" + SidPlaceholder + "\",\"op\":\"getArticle\",\"article_id\":" + id + "}";
                    ResponseArray articleResp = await SendRequestArrayAsync(null, getArticle);
                    Article article = ParseContentOrError<Article>(articleResp)[0];
                    if (forceRefresh)
                        ArticleCache.Remove(id);
                    try
                    {
                        ArticleCache.Add(id, article);
                    }
                    catch
                    {
                        Logger.WriteLine("Got Article twice. First won.");
                    }
                }
                else
                {
                    Logger.WriteLine("ARTICLE got from Cache: " + id);
                }
                return ArticleCache[id];
            }
            catch (TtRssException e)
            {
                throw e;
            }
        }

        public async Task<Config> getConfig(bool refresh)
        {
            try
            {
                if (refresh || Config == null)
                {
                    string updateOp = "{\"sid\":\"" + SidPlaceholder + "\",\"op\":\"getConfig\"}";
                    Response response = await SendRequestAsync(null, updateOp);
                    Config = ParseContentOrError<Config>(response);
                }
                return Config;
            }
            catch (TtRssException e)
            {
                throw e;
            }
        }

        public async Task<bool> updateArticles(IEnumerable<int> ids, UpdateField field, UpdateMode mode)
        {
            try
            {
                string updateOp = "{\"sid\":\"" + SidPlaceholder + "\",\"op\":\"updateArticle\",\"article_ids\":\"" + string.Join(",", ids.Select(n => n.ToString()).ToArray()) + "\",\"mode\":" + (int)mode + ",\"field\":" + (int)field + "}";
                Response response = await SendRequestAsync(null, updateOp);
                if (response.content.ToString().Contains("OK"))
                {
                    return true;
                }
                return false;
            }
            catch (TtRssException e)
            {
                throw e;
            }
        }

        public Feed getFeedById(int? id)
        {
            if (!id.HasValue)
            {
                return null;
            }
            try
            {
                return FeedDictionary[id.Value];
            }
            catch (KeyNotFoundException)
            {
                return null;
            }
        }

        public async Task<List<Feed>> getFeeds(bool reload)
        {
            if (reload || FeedDictionary.Count == 0)
            {
                try
                {
                    FeedDictionary.Clear();
                    Logger.WriteLine("FEEDS got through API.");
                    string getFeeds = "{\"sid\":\"" + SidPlaceholder + "\",\"op\":\"getFeeds\",\"cat_id\":-3,\"unread_only\":false}";
                    ResponseArray response = await SendRequestArrayAsync(null, getFeeds);
                    List<Feed> feeds = ParseContentOrError<Feed>(response);
                    feeds.ForEach(x => FeedDictionary.Add(x.id, x));
                }
                catch (TtRssException e)
                {
                    throw e;
                }
            }
            else
            {
                Logger.WriteLine("FEEDS got through Cache.");
            }
            return FeedDictionary.Values.ToList<Feed>();
        }

        public async Task<List<Feed>> getFeeds()
        {
            return await getFeeds(false);
        }

        public async Task<List<Category>> getCategories()
        {
            string getCategories = "{\"sid\":\"" + SidPlaceholder + "\",\"op\":\"getCategories\",\"unread_only\":false,\"enable_nested\":false,\"include_empty\":true}";
            try
            {
                ResponseArray response = await SendRequestArrayAsync(null, getCategories);
                List<Category> cats = ParseContentOrError<Category>(response);
                return cats;
            }
            catch (TtRssException e)
            {
                throw e;
            }
        }

        public async Task<Response> SendRequestAsync(string server, string requestUrl)
        {
            if (sessionId == null && !requestUrl.Contains("\"op\":\"login\""))
            {
                await Login(false);
            }
            requestUrl = requestUrl.Replace(SidPlaceholder, sessionId);
            Logger.WriteLine("API call: " + requestUrl);

            try
            {
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

                HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();
                using (var sr = new StreamReader(response.GetResponseStream()))
                {
                    string responseString = sr.ReadToEnd();
                    if (responseString.Length == 0)
                    {
                        throw new TtRssException(NONETWORKERROR);
                    }
                    Logger.WriteLine("API Response: " + responseString);
                    Response obj = JsonConvert.DeserializeObject<Response>(responseString);
                    if (obj != null)
                    {
                        return obj;
                    }
                    else
                    {
                        throw new TtRssException("Error occured: JSON Deserialization returned null.");
                    }
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
                await Login(false);
            }
            requestUrl = requestUrl.Replace(SidPlaceholder, sessionId);
            Logger.WriteLine("API call: " + requestUrl);
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

            HttpWebResponse httpResponse = (HttpWebResponse)await request.GetResponseAsync();
            using (var sr = new StreamReader(httpResponse.GetResponseStream()))
            {
                string responseString = sr.ReadToEnd();
                if (responseString.Length == 0)
                {
                    throw new TtRssException(NONETWORKERROR);
                }
                Logger.WriteLine("API Response: " + responseString);
                try
                {
                    ResponseArray obj = JsonConvert.DeserializeObject<ResponseArray>(responseString);
                    if (obj != null)
                    {
                        return obj;
                    }
                    else
                    {
                        throw new TtRssException("Error occured: JSON Deserialization returned null.");
                    }
                }
                catch (JsonSerializationException e)
                {
                    Response response = JsonConvert.DeserializeObject<Response>(responseString);
                    Error error = response.getContent<Error>();
                    throw new TtRssException("Error occured: " + error.error, e);
                }
            }
        }

        public T ParseContentOrError<T>(Response response)
        {
            string contentString = response.content.ToString();
            if (response.status == 1)
            {
                Error error = response.getContent<Error>();
                throw new TtRssException("Error occured: " + error.error);
            }
            else
            {
                return response.getContent<T>();
            }
        }

        public List<T> ParseContentOrError<T>(ResponseArray response)
        {
            string contentString = response.content.ToString();
            if (response.status == 1)
            {
                Error error = JsonConvert.DeserializeObject<Error>(contentString);
                throw new TtRssException("Error occured: " + error.error);
            }
            else
            {
                return response.getContentAsList<T>();
            }
        }
    }
}
