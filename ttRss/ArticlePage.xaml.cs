using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using TinyTinyRSS.Classes;
using TinyTinyRSS.Interface;
using TinyTinyRSS.Interface.Classes;
using TinyTinyRSSInterface.Classes;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace TinyTinyRSS
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ArticlePage : AbstractArticlePage
    {
        private int _selectedIndex;
        private int _lastPivotIndex;

        public ArticlePage()
        {
            this.Loaded += PageLoaded;
            InitializeComponent();
            ArticlesCollection = new ObservableCollection<WrappedArticle>();
            _showUnreadOnly = ConnectionSettings.getInstance().showUnreadOnly;
            _sortOrder = ConnectionSettings.getInstance().sortOrder;
            _moreArticles = true;
            _moreArticlesLoading = false;
            _selectedIndex = 0;
            _lastPivotIndex = -1;
            RegisterForShare();
            SystemNavigationManager.GetForCurrentView().BackRequested += App_BackRequested;
            if (!ConnectionSettings.getInstance().progressAsCntr)
            {
                Scrollbar.Visibility = Visibility.Collapsed;
                Counter.Visibility = Visibility.Visible;
            }
            else
            {
                Scrollbar.IsIndeterminate = false;
            }
        }

        protected override ProgressRing getProgressRing()
        {
            return this.ArticleProgressBar;
        }
        protected override ProgressBar getMarkProgressBar()
        {
            return MarkArticleProgressBar;
        }
        protected override ProgressBar getMultipleMarkProgressBar()
        {
            return MarkArticleProgressBar;
        }        
        protected override TextBlock getProgressRingText()
        {
            return ArticleProgressBarText;
        }

        public void App_BackRequested(object sender, BackRequestedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            if (rootFrame != null && rootFrame.CanGoBack)
            {
                e.Handled = true;
                if (initialized)
                {
                    NavigationObject parameter = new NavigationObject();
                    parameter.selectedIndex = _selectedIndex;
                    parameter._showUnreadOnly = _showUnreadOnly;
                    parameter._sortOrder = _sortOrder;
                    parameter.ArticlesCollection = new ObservableCollection<WrappedArticle>();
                    foreach (WrappedArticle article in ArticlesCollection)
                    {
                        parameter.ArticlesCollection.Add(article);
                    }
                    Frame.Navigate(typeof(MainPage), parameter);
                }
                else
                {
                    rootFrame.GoBack();
                }
            }
        }

        private async void PageLoaded(object sender, RoutedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
                    rootFrame.CanGoBack ?
                        AppViewBackButtonVisibility.Visible :
                        AppViewBackButtonVisibility.Collapsed;
            if (!initialized)
            {
                bool result = await LoadHeadlines();
                if (!result)
                {
                    return;
                }
                _selectedIndex = 0;
                PivotControl_LoadingPivotItem(null, new PivotItemEventArgs());
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is NavigationObject)
            {
                initialized = true;
                NavigationObject nav = e.Parameter as NavigationObject;
                _selectedIndex = nav.selectedIndex;
                _sortOrder = nav._sortOrder;
                _showUnreadOnly = nav._showUnreadOnly;
                ArticlesCollection = new ObservableCollection<WrappedArticle>();
                foreach (WrappedArticle article in nav.ArticlesCollection)
                {
                    ArticlesCollection.Add(article);
                }
                channel.LogMessage("NavigatedTo ArticlePage from ListView for Feed " + ConnectionSettings.getInstance().selectedFeed);
            }
            else {
                initialized = false;
                channel.LogMessage("NavigatedTo ArticlePage for Feed " + ConnectionSettings.getInstance().selectedFeed);
            }
            base.OnNavigatedTo(e);
        }

        private async void PivotControl_LoadingPivotItem(object sender, PivotItemEventArgs e)
        {
            if (ArticlesCollection.Count <= _selectedIndex)
            {
                return;
            }
            if (e.Item == null)
            {
                e.Item = Item0;
            }
            try
            {
                if (PivotControl.SelectedIndex == 0 && _lastPivotIndex == -1)
                {
                    _lastPivotIndex = 0;
                }
                else if (forwardNavigated())
                {
                    _selectedIndex = positiveMod(_selectedIndex + 1, ArticlesCollection.Count);
                }
                else
                {
                    _selectedIndex = positiveMod(_selectedIndex - 1, ArticlesCollection.Count);
                }
                int localSelected = _selectedIndex;
                SetProgressBar(true, ProgressMsg.LoadArticle);
                updateCount(false);

                WrappedArticle item = ArticlesCollection[_selectedIndex];
                Article article = null;
                AppBar.DataContext = item;
                e.Item.DataContext = item;
                try
                {
                    Task<Article> getContentTask = item.getContent();
                    setCommandBarChecked(item.Headline);
                    article = await getContentTask;
                }
                catch (TtRssException)
                {
                    channel.LogMessage("error loading content for article.");
                }
                if (article != null && _selectedIndex == localSelected)
                {
                    setHtml(article.content);
                }
                e.Item.UpdateLayout();
                SetProgressBar(false, ProgressMsg.LoadArticle);
                if (await markArticleReadAutomatically(article))
                {
                    item.Headline.unread = false;
                    setCommandBarChecked(item.Headline);
                }
                if (_selectedIndex <= ArticlesCollection.Count - 1 && _selectedIndex > ArticlesCollection.Count - 3)
                {
                    await LoadMoreHeadlines();
                }
            }
            catch (TtRssException ex)
            {
                SetProgressBar(false, ProgressMsg.LoadArticle);
                checkException(ex);
            }
        }

        /// <summary>
        /// Fix IsChecked Property after Binding change
        /// </summary>
        /// <param name="headline">Headline holding data</param>
        private void setCommandBarChecked(Headline headline)
        {
            toggleStarAppBarButton.IsChecked = headline.marked;
            toogleReadAppBarButton.IsChecked = headline.unread;
        }

        private void setHtml(string content)
        {
            PivotItem myPivotItem =
                (PivotItem)(PivotControl.ContainerFromItem(PivotControl.Items[PivotControl.SelectedIndex]));

            var wc = Helper.FindDescendantByName(myPivotItem, "WebContent") as WebView;
            if (wc != null)
            {
                int margin = ConnectionSettings.getInstance().swipeMargin;
                wc.Margin = new Thickness(margin, 0, margin, 0);
                wc.NavigateToString(content);
                wc.Visibility = Visibility.Visible;
            }
            else
            {
                channel.LogMessage("WebBrowser not found");
            }
        }

        protected override async void updateCount(bool force)
        {
            int actual = _selectedIndex + 1;
            if (ConnectionSettings.getInstance().selectedFeed<0 || (_showUnreadOnly.HasValue && _showUnreadOnly.Value))
            {
                int max;
                if (ConnectionSettings.getInstance().selectedFeed == (int)FeedId.Fresh)
                {
                    max = await TtRssInterface.getInterface().getUnReadCount(force);
                }
                else
                {
                    int ifOfFeed = ConnectionSettings.getInstance().selectedFeed;
                    if (ifOfFeed == (int)FeedId.RecentlyRead)
                    {
                        ifOfFeed = (int)FeedId.All;
                    }
                    max = await TtRssInterface.getInterface().getCountForFeed(force, ifOfFeed);
                }
                if (ArticlesCollection.Count > max)
                {
                    max = ArticlesCollection.Count;
                }
                if (Scrollbar.Maximum < max || force)
                {
                    Scrollbar.Maximum = max;
                }
                Counter.Text = actual + "/" + Scrollbar.Maximum;
            }
            else
            {
                string max = Helper.AppendPlus(_moreArticles, ArticlesCollection.Count + "");
                Counter.Text = actual + "/" + max;
                if (_moreArticles)
                {
                    Scrollbar.Maximum = ArticlesCollection.Count + 1;
                }
                else
                {
                    Scrollbar.Maximum = ArticlesCollection.Count;
                }
            }
            Scrollbar.Value = actual;
        }

        private async void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateField field;
            int selectedIndex = _selectedIndex;
            FrameworkElement element = (FrameworkElement) sender;
            Article current = await ArticlesCollection[selectedIndex].getContent();
            if ("publishAppBarMenu".Equals(element.Name))
            {
                field = UpdateField.Published;
            }
            else if ("toggleStarAppBarButton".Equals(element.Name))
            {
                field = UpdateField.Starred;
            }
            else if ("toogleReadAppBarButton".Equals(element.Name))
            {
                field = UpdateField.Unread;
            }
            else if ("markAllReadMenu".Equals(element.Name))
            {
                try
                {
                    bool success = await markAllRead();
                    if (success)
                    {
                        foreach (WrappedArticle wa in ArticlesCollection)
                        {
                            if (wa.Headline.unread)
                            {
                                wa.Headline.unread = false;
                            }
                            if (wa.Article != null && wa.Article.unread)
                            {
                                wa.Article.unread = false;
                            }
                        }
                    }
                }
                catch (TtRssException ex)
                {
                    checkException(ex);
                }
                return;
            }
            else
            {
                return;
            }
            try
            {
                SetProgressBar(true, ProgressMsg.MarkArticle);
                List<int> idList = new List<int>();
                idList.Add(current.id);
                bool success = await TtRssInterface.getInterface().updateArticles(idList, field, UpdateMode.Toggle);
                if (success)
                {
                    ArticlesCollection[selectedIndex].Article = await TtRssInterface.getInterface().getArticle(current.id, true);
                    ArticlesCollection[selectedIndex].Headline.unread = ArticlesCollection[selectedIndex].Article.unread;
                    ArticlesCollection[selectedIndex].Headline.published = ArticlesCollection[selectedIndex].Article.published;
                    ArticlesCollection[selectedIndex].Headline.marked = ArticlesCollection[selectedIndex].Article.marked;

                    if ("toogleReadAppBarButton".Equals(element.Name))
                    {
                        await PushNotificationHelper.UpdateLiveTile(-1);
                    }
                }
                SetProgressBar(false, ProgressMsg.MarkArticle);
            }
            catch (TtRssException ex)
            {
                checkException(ex);
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            SystemNavigationManager.GetForCurrentView().BackRequested -= App_BackRequested;
            base.OnNavigatedFrom(e);
        }

        private bool forwardNavigated()
        {
            // fw: 0-1,1-2,2-0
            // bw: 2-1,1-0,0-2
            int actual = PivotControl.SelectedIndex;
            int last = _lastPivotIndex;
            _lastPivotIndex = actual;
            if (actual != 2 && last != 2 && last > actual)
            {
                return false;
            }
            else if (actual == 2 && last == 0)
            {
                return false;
            }
            else if (actual == 1 && last == 2)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        private int positiveMod(int x, int m)
        {
            int r = x % m;
            return r < 0 ? r + m : r;
        }

        private void resetPivot(object sender, PivotItemEventArgs e)
        {
            var wc = Helper.FindDescendantByName(e.Item, "WebContent") as WebView;
            if (wc != null)
            {
                wc.NavigateToString("");
                wc.Visibility = Visibility.Collapsed;
            }
            else
            {
                channel.LogMessage("WebBrowser not found");
            }
        }

        protected override int getSelectedIdx()
        {
            return _selectedIndex;
        }

        private void Icon_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            Image img = sender as Image;
            if (img != null)
            {
                img.Visibility = Visibility.Collapsed;
            }
        }
    }
}
