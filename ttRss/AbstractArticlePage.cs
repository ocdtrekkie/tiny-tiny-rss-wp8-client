using CaledosLab.Portable.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using TinyTinyRSS.Classes;
using TinyTinyRSS.Interface;
using TinyTinyRSS.Interface.Classes;
using TinyTinyRSSInterface.Classes;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace TinyTinyRSS
{
    public abstract class AbstractArticlePage : Page
    {
        protected Collection<WrappedArticle> ArticlesCollection;
        protected ResourceLoader loader = new Windows.ApplicationModel.Resources.ResourceLoader();
        protected bool _showUnreadOnly, _moreArticles, _moreArticlesLoading;
        protected int _sortOrder;
        protected bool initialized;

        protected enum ProgressMsg { LoadHeadlines, LoadMoreHeadlines, MarkArticle, MarkMultipleArticle, LoadArticle, LoginProgress };
        protected Dictionary<FrameworkElement, List<ProgressMsg>> activeInProgress { get; set; }

        protected abstract void updateCount(bool p);
        protected abstract int getSelectedIdx();
        protected abstract ProgressRing getProgressRing();
        protected abstract ProgressBar getMarkProgressBar();
        protected abstract ProgressBar getMultipleMarkProgressBar();
        protected abstract TextBlock getProgressRingText();

        public AbstractArticlePage()
        {
            activeInProgress = new Dictionary<FrameworkElement, List<ProgressMsg>>();
        }

        protected void ShareAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            DataTransferManager.ShowShareUI();
        }

        protected void RegisterForShare()
        {
            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested += new TypedEventHandler<DataTransferManager,
                DataRequestedEventArgs>(this.ShareLinkHandler);
        }

        protected void ShareLinkHandler(DataTransferManager sender, DataRequestedEventArgs e)
        {
            Headline head = ArticlesCollection[getSelectedIdx()].Headline;
            DataRequest request = e.Request;
            request.Data.Properties.Description = "Shared by tt-RSS Reader for Windows Phone.";
            request.Data.Properties.Title = head.title;
            request.Data.SetWebLink(new Uri(head.link));
        }

        /// <summary>
        /// Handle different actions shown in ProgressRings + Text
        /// </summary>
        protected void SetProgressBar(bool on, ProgressMsg message)
        {
            try {
                FrameworkElement key;
                TextBlock textBlock = null;
                if (message.Equals(ProgressMsg.MarkArticle) || message.Equals(ProgressMsg.LoadArticle))
                {
                    key = getMarkProgressBar();
                }
                else if (message.Equals(ProgressMsg.MarkMultipleArticle))
                {
                    key = getMultipleMarkProgressBar();
                }
                else {
                    key = getProgressRing();
                    textBlock = getProgressRingText();
                }
                if (!activeInProgress.ContainsKey(key))
                {
                    var list = new List<ProgressMsg>();
                    activeInProgress.Add(key, list);
                }

                List<ProgressMsg> set;
                if (on)
                {
                    if (key is ProgressBar) {
                        key.Visibility = Visibility.Visible;
                    }
                    else {
                        StackPanel parent = (StackPanel)key.Parent;
                        parent.Visibility = Visibility.Visible;
                        ((ProgressRing)key).IsActive = true;
                        if (activeInProgress.TryGetValue(key, out set))
                        {
                            set.Add(message);
                        }
                        string msg = loader.GetString(message.ToString());
                        if (msg != null)
                        {
                            textBlock.Text = msg;
                        }
                    }
                }
                else {
                    if (activeInProgress.TryGetValue(key, out set))
                    {
                        set.Remove(message);
                        if (set.Count > 0)
                        {
                            ProgressMsg old = set.First();
                            string msgOld = loader.GetString(old.ToString());
                            if (msgOld != null && textBlock != null)
                            {
                                textBlock.Text = msgOld;
                            }
                        }
                        else
                        {
                            if (key is ProgressBar) {
                                key.Visibility = Visibility.Collapsed;
                            }
                            else {
                                StackPanel parent = (StackPanel)key.Parent;
                                parent.Visibility = Visibility.Collapsed;
                                ((ProgressRing)key).IsActive = false;
                                textBlock.Text = "";
                            }
                        }
                    }
                    else
                    {
                        if (key is ProgressBar) {
                            key.Visibility = Visibility.Collapsed;
                        }
                        else {
                            StackPanel parent = (StackPanel)key.Parent;
                            parent.Visibility = Visibility.Collapsed;
                            ((ProgressRing)key).IsActive = false;
                            textBlock.Text = "";
                        }
                    }
                }
            } catch (NullReferenceException e)
            {
                Logger.WriteLine("Nre in SetProgressBar - " + e.Message);
            }
        }

        protected async void openExt_Click(object sender, RoutedEventArgs e)
        {
            if (getSelectedIdx() < 0)
            {
                return;
            }
            WrappedArticle article = ArticlesCollection[getSelectedIdx()];
            if (article.Article != null)
            {
                var uri = new Uri(article.Article.link);
                await Windows.System.Launcher.LaunchUriAsync(uri);
            }
            else
            {
                Article art = await article.getContent();
                var uri = new Uri(art.link);
                await Windows.System.Launcher.LaunchUriAsync(uri);
            }
        }

        /// <summary>
        /// Get's the headlines of the shown feed from tt-rss. 
        /// Depending on the settings, may only unread articles are loaded.
        /// </summary>
        /// <returns>true, cause void Tasks don't work.</returns>
        protected async Task<bool> LoadHeadlines()
        {
            try
            {
                int feedId = ConnectionSettings.getInstance().selectedFeed;
                bool _isCat = ConnectionSettings.getInstance().isCategory;
                SetProgressBar(true, ProgressMsg.LoadHeadlines);
                ArticlesCollection.Clear();
                List<Headline> headlines = await TtRssInterface.getInterface().getHeadlines(feedId, _showUnreadOnly, 0, _sortOrder, _isCat);
                if (headlines.Count == 0)
                {
                    _moreArticles = false;
                    MessageDialog msgbox = new MessageDialog(loader.GetString("NoArticlesMessage"));
                    await msgbox.ShowAsync();
                    Frame rootFrame = Window.Current.Content as Frame;
                    if (rootFrame.CanGoBack)
                    {
                        rootFrame.GoBack();
                    }
                    else
                    {
                        if (!(this is MainPage))
                        {
                            Frame.Navigate(typeof(MainPage));
                        }
                    }
                    return false;
                }
                else
                {
                    if (ConnectionSettings.getInstance().selectedFeed == feedId)
                    {
                        ArticlesCollection = new ObservableCollection<WrappedArticle>();
                        foreach (Headline h in headlines)
                        {
                            ArticlesCollection.Add(new WrappedArticle(h));
                        }
                        updateCount(false);
                    }
                }
            }
            catch (TtRssException ex)
            {
                checkException(ex);
                return false;
            }
            finally
            {
                SetProgressBar(false, ProgressMsg.LoadHeadlines);
            }
            return true;
        }

        protected async void checkException(TtRssException ex)
        {
            if (ex.Message.Equals(TtRssInterface.NONETWORKERROR))
            {
                MessageDialog msgbox = new MessageDialog(loader.GetString("NoConnection"));
                await msgbox.ShowAsync();
                Frame rootFrame = Window.Current.Content as Frame;
                if (rootFrame != null && rootFrame.CanGoBack)
                {
                    rootFrame.GoBack();
                }
                else
                {
                    Frame.Navigate(typeof(MainPage));
                }
            }
            else
            {
                throw ex;
            }
        }

        /// <summary>
        /// When a pivot item is loaded check if you need to load more articles, cause of lazy loading.
        /// </summary>
        protected async Task LoadMoreHeadlines()
        {
            if (_moreArticles && !_moreArticlesLoading)
            {
                try
                {
                    _moreArticlesLoading = true;
                    int feedId = ConnectionSettings.getInstance().selectedFeed;
                    bool _isCat = ConnectionSettings.getInstance().isCategory;
                    SetProgressBar(true, ProgressMsg.LoadMoreHeadlines);
                    
                    // First get new items if existing
                    List<Headline> headlines = await TtRssInterface.getInterface().getHeadlines(feedId, _showUnreadOnly, 0, _sortOrder, _isCat);

                    if (headlines.Count <= 0)
                    {
                        _moreArticles = false;
                    }
                    else
                    {
                        bool newItems = false;
                        foreach (Headline h in headlines)
                        {

                            if (!isHeadlineInArticleCollection(h) && ConnectionSettings.getInstance().selectedFeed == feedId)
                            {
                                ArticlesCollection.Add(new WrappedArticle(h));
                                newItems = true;
                            }
                        }
                        _moreArticles = newItems;
                        updateCount(false);
                    }
                    // Then check if there are more items at the end.
                    int skip = ArticlesCollection.Count;
                    if (feedId<0)
                    {
                        switch (feedId)
                        {
                            case -3:
                                ArticlesCollection.Count(e => e.Headline.unread);
                                break;
                            case -1:
                                ArticlesCollection.Count(e => e.Headline.marked);
                                break;
                            case -2:
                                ArticlesCollection.Count(e => e.Headline.published);
                                break;
                            default: break;
                        }
                    }
                    else if (_showUnreadOnly)
                    {
                        skip = ArticlesCollection.Count(e => e.Headline.unread);
                    }

                    List<Headline> headlinesAfter = await TtRssInterface.getInterface().getHeadlines(feedId, _showUnreadOnly, skip, _sortOrder, _isCat);
                    if (headlinesAfter.Count > 0)
                    {
                        bool newItems = false;
                        foreach (Headline h in headlinesAfter)
                        {

                            if (!isHeadlineInArticleCollection(h) && ConnectionSettings.getInstance().selectedFeed == feedId)
                            {
                                ArticlesCollection.Add(new WrappedArticle(h));
                                newItems = true;
                            }
                        }
                        updateCount(false);
                        _moreArticles = _moreArticles || newItems;
                    }
                }
                catch (TtRssException ex)
                {
                    checkException(ex);
                }
                finally
                {
                    _moreArticlesLoading = false;
                    SetProgressBar(false, ProgressMsg.LoadMoreHeadlines);
                }
            }
        }

        private bool isHeadlineInArticleCollection(Headline headline)
        {
            bool contains = false;
            foreach (WrappedArticle a in ArticlesCollection)
            {
                if (a.Headline.id == headline.id)
                {
                    contains = true;
                    break;
                }
            }
            return contains;
        }
        
        protected async Task<bool> markAllRead() 
        {
            SetProgressBar(true, ProgressMsg.MarkArticle);
            try {
                bool success = await TtRssInterface.getInterface().markAllArticlesRead(ConnectionSettings.getInstance().selectedFeed, ConnectionSettings.getInstance().isCategory);
                PushNotificationHelper.UpdateLiveTile(-1);
                SetProgressBar(false, ProgressMsg.MarkArticle);
                return success;
            }
            catch (TtRssException ex)
            {
                checkException(ex);
                SetProgressBar(false, ProgressMsg.MarkArticle);
            }
            return false;        
        }

        protected async Task<bool> markArticleReadAutomatically(Article article)
        {
            if (ConnectionSettings.getInstance().markRead && article != null && article.unread)
            {
                List<int> idList = new List<int>();
                idList.Add(article.id);
                try
                {
                    bool success = await TtRssInterface.getInterface().updateArticles(idList, UpdateField.Unread, UpdateMode.False);

                    if (success)
                    {
                        Task tsk = PushNotificationHelper.UpdateLiveTile(-1);
                        article.unread = false;
                        await tsk;
                        return true;
                    }
                }
                catch (TtRssException ex)
                {
                    checkException(ex);
                }
            }
            return false;
        }
    }
}
