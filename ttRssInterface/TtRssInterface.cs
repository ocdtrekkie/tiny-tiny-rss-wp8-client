using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using TinyTinyRSS.Interface.Classes;
using System.Threading.Tasks;
using TinyTinyRSSInterface.Classes;
using Windows.Web.Http;
using Windows.Web.Http.Headers;
using CaledosLab.Portable.Logging;
using Windows.Web.Http.Filters;
using Windows.Security.Cryptography.Certificates;

namespace TinyTinyRSS.Interface
{
    public class TtRssInterface
    {
        public const int INITIALHEADLINECOUNT = 20;
        public const int ADDITIONALHEADLINECOUNT = 10;
        public const string NONETWORKERROR = "HTTP Response is null.";

        private static TtRssInterface instance;
        private string sessionId;
        private Dictionary<int, Feed> FeedDictionary;
        private Dictionary<int, int> GlobalCounter;
        private Dictionary<int, int> FeedCounter;
        private Dictionary<int, int> CategoryCounter;
        public Config Config { get; set; }
        private static string SidPlaceholder = "StringToReplaceBySessionId";

        private TtRssInterface()
        {
            FeedDictionary = new Dictionary<int, Feed>();
            //ArticleCache = new LimitedSizeDictionary<int, Article>(20);
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
            catch (NullReferenceException nre)
            {
                return "Error sending http request. Check server field.";
            }
        }

        public async Task<bool> CheckLogin()
        {
            if (sessionId != null)
            {
                return true;
            } else if(ConnectionSettings.getInstance().server.Length == 0) {
                return false;
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

        public async Task<int> getUnReadCount(bool force)
        {
            return await getCountForFeed(force, (int)FeedId.Fresh);
        }

        public async Task<int> getCounters()
        {
            try
            {
                string request = "{\"sid\":\"" + SidPlaceholder + "\",\"op\":\"getCounters\"}";
                ResponseArray response = await SendRequestArrayAsync(null, request);
                List<Counter> counters = ParseContentOrError<Counter>(response);
                foreach (Counter c in counters)
                {
                    int parsedId;
                    if (c.id.Equals("global-unread"))
                    {
                        GlobalCounter[0] = c.counter; // The one of getCounters does not count "old" articles
                    }
                    else if (c.id.Equals("subscribed-feeds"))
                    {
                        GlobalCounter[1] = c.counter;
                    }
                    else if (int.TryParse(c.id, out parsedId))
                    {
                        if (c.kind != null && c.kind.Equals("cat"))
                        {
                            CategoryCounter[parsedId] = c.counter;
                        }
                        else
                        {
                            if (parsedId <= 0 && parsedId > -3)
                            {
                                FeedCounter[parsedId] = int.Parse(c.auxcounter);
                            }
                            else
                            {
                                FeedCounter[parsedId] = c.counter;
                            }
                        }
                    }
                }
                return counters.Count;
            }
            catch (ArgumentException e)
            {
                return FeedCounter.Count;
            }
            catch (TtRssException e)
            {
                throw e;
            }
        }

        public async Task<int> getCountForFeed(bool forceUpdate, int feedId)
        {
            try
            {
                if (forceUpdate || !FeedCounter.ContainsKey(feedId))
                {
                    await getCounters();
                }
                return FeedCounter[feedId];
            }
            catch (KeyNotFoundException e)
            {
                Logger.WriteLine(e.StackTrace);
                return 0;
            }
            catch (TtRssException e)
            {
                Logger.WriteLine(e.StackTrace);
                return 0;
            }
        }

        public async Task<int> getCountForCategory(bool forceUpdate, int feedId)
        {
            if (forceUpdate || !CategoryCounter.ContainsKey(feedId))
            {
                await getCounters();
            }
            return CategoryCounter[feedId];
        }

        public async Task<List<Headline>> getHeadlines(int feedId, bool unreadOnly, int skip, int sortOrder, bool isCat)
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
                string getHeadlines = "{\"sid\":\"" + SidPlaceholder + "\",\"op\":\"getHeadlines\",\"show_excerpt\":false,\"limit\":" + limit + ",\"skip\":" + skip + ", \"view_mode\":\"" + view_mode + "\", \"feed_id\":" + (int)feedId + ", \"order_by\":\"" + sort + "\", \"is_cat\":\"" + isCat + "\"}";
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
                string getArticle = "{\"sid\":\"" + SidPlaceholder + "\",\"op\":\"getArticle\",\"article_id\":" + id + "}";
                ResponseArray articleResp = await SendRequestArrayAsync(null, getArticle);
                Article article = ParseContentOrError<Article>(articleResp)[0];
                return article;
            }
            catch (TtRssException e)
            {
                return null;
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

        public async Task<bool> markAllArticlesRead(int feedId)
        {
             string catchUp = "{\"sid\":\"" + SidPlaceholder + "\",\"op\":\"catchupFeed\",\"feed_id\":" + (int)feedId + "}";
             Response status = await SendRequestAsync(null, catchUp);
             if (status.content.ToString().Contains("OK"))
             {
                 return true;
             }
             return false;
               
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
                    foreach (Feed feed in feeds)
                    {
                        FeedDictionary[feed.id] = feed;
                    }
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
            return await SendRequestAsync(server, requestUrl, false);
        }

        public async Task<Response> SendRequestAsync(string server, string requestUrl, bool second)
        {
            if (sessionId == null && !requestUrl.Contains("\"op\":\"login\""))
            {
                await Login(false);
            }
            requestUrl = requestUrl.Replace(SidPlaceholder, sessionId);
            bool retry = false;
            try
            {
                if (server == null)
                {
                    server = ConnectionSettings.getInstance().server;
                }
                // Create a Base Protocol Filter to add certificate errors I want to ignore...
                var allowSelfSigned = new HttpBaseProtocolFilter();
                // Untrused because this is a self signed cert that is not installed
                if (ConnectionSettings.getInstance().allowSelfSignedCert)
                {
                    allowSelfSigned.IgnorableServerCertificateErrors.Add(ChainValidationResult.Untrusted);
                }
                HttpClient httpClient = new HttpClient(allowSelfSigned);
                HttpRequestMessage msg = new HttpRequestMessage(new HttpMethod("POST"), new Uri(server));
                msg.Content = new HttpStringContent(requestUrl);
                msg.Content.Headers.ContentType = new HttpMediaTypeHeaderValue("application/json");
                HttpResponseMessage httpresponse = await httpClient.SendRequestAsync(msg).AsTask();
                var responseString = await httpresponse.Content.ReadAsStringAsync();

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
            catch (Exception ex)
            {
                if (!second)
                {
                    retry = true;
                } else {				
					Logger.WriteLine(ex.Message);
				}
            }
            if (retry)
            {
                return await SendRequestAsync(server, requestUrl, true);
            }
            else
            {
                Logger.WriteLine("Exception twice in SendRequestAsync.");
                return null;
            }
        }

        public async Task<ResponseArray> SendRequestArrayAsync(string server, string requestUrl)
        {
            return await SendRequestArrayAsync(server, requestUrl, false);
        }

        public async Task<ResponseArray> SendRequestArrayAsync(string server, string requestUrl, bool second)
        {
            bool retry = false;
            try
            {
                if (sessionId == null && !requestUrl.Contains("\"op\":\"login\""))
                {
                    await Login(false);
                }
                requestUrl = requestUrl.Replace(SidPlaceholder, sessionId);
                if (server == null)
                {
                    server = ConnectionSettings.getInstance().server;
                }
                // Create a Base Protocol Filter to add certificate errors I want to ignore...
                var allowSelfSigned = new HttpBaseProtocolFilter();
                // Untrused because this is a self signed cert that is not installed
                if (ConnectionSettings.getInstance().allowSelfSignedCert)
                {
                    allowSelfSigned.IgnorableServerCertificateErrors.Add(ChainValidationResult.Untrusted);
                }
                HttpClient httpClient = new HttpClient(allowSelfSigned);
                HttpRequestMessage msg = new HttpRequestMessage(new HttpMethod("POST"), new Uri(server));
                msg.Content = new HttpStringContent(requestUrl);
                msg.Content.Headers.ContentType = new HttpMediaTypeHeaderValue("application/json");
                HttpResponseMessage httpresponse = await httpClient.SendRequestAsync(msg).AsTask();
                var responseString = await httpresponse.Content.ReadAsStringAsync();

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
            catch (Exception ex)
            {
                if (!second)
                {
                    retry = true;
                } else {				
					Logger.WriteLine(ex.Message);
				}
            }
            if (retry)
            {
                return await SendRequestArrayAsync(server, requestUrl, true);
            }
            else
            {
                Logger.WriteLine("NullReferenceException twice in SendRequestArrayAsync.");
                throw new TtRssException(NONETWORKERROR);
            }
        }

        public T ParseContentOrError<T>(Response response)
        {
            if (response == null)
            {
                return default(T);
            }
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
