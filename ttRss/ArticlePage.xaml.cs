﻿using CaledosLab.Portable.Logging;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using TinyTinyRSS.Classes;
using TinyTinyRSS.Interface;
using TinyTinyRSS.Interface.Classes;
using TinyTinyRSSInterface.Classes;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
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
            SystemNavigationManager.GetForCurrentView().BackRequested += App_BackRequested;
            ArticlesCollection = new ObservableCollection<WrappedArticle>();
            _showUnreadOnly = ConnectionSettings.getInstance().showUnreadOnly;
            _sortOrder = ConnectionSettings.getInstance().sortOrder;
            _moreArticles = true;
            _moreArticlesLoading = false;
            _selectedIndex = 0;
            _lastPivotIndex = -1;
            RegisterForShare();
            if (!ConnectionSettings.getInstance().progressAsCntr)
            {
                Scrollbar.Visibility = Visibility.Collapsed;
                Counter.Visibility = Visibility.Visible;
            }
            else
            {
                Scrollbar.IsIndeterminate = false;
            }
            BuildLocalizedApplicationBar();
        }

        private void App_BackRequested(object sender, BackRequestedEventArgs e)
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
                    Frame.Navigate(typeof(HeadlinesPage), parameter);
                }
                else
                {
                    rootFrame.GoBack();
                }
            }
        }

        private async void PageLoaded(object sender, RoutedEventArgs e)
        {
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
            if(e.Parameter is NavigationObject) {
                initialized = true;
                NavigationObject nav = e.Parameter as NavigationObject;
                _selectedIndex = nav.selectedIndex;
                _sortOrder = nav._sortOrder;
                _showUnreadOnly = nav._showUnreadOnly;
                ArticlesCollection = nav.ArticlesCollection;
                BuildLocalizedApplicationBar();
                Logger.WriteLine("NavigatedTo ArticlePage from ListView for Feed " + ConnectionSettings.getInstance().selectedFeed);
            } else {
                initialized = false;
                Logger.WriteLine("NavigatedTo ArticlePage for Feed " + ConnectionSettings.getInstance().selectedFeed);
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
                SetProgressBar(true);
                updateCount(false);

                WrappedArticle item = ArticlesCollection[_selectedIndex];
                e.Item.DataContext = item;
                Article article = null;
                try
                {
                    article = await item.getContent();
                }
                catch (TtRssException)
                {
                    Logger.WriteLine("error loading content for article.");
                }
                if (article != null && _selectedIndex == localSelected)
                {
                    setHtml(article.content);
                    /*var icon = Helper.FindDescendantByName(e.Item, "Icon") as Image;
                    if (icon != null)
                    {
                        Feed articlesFeed = TtRssInterface.getInterface().getFeedById(item.Headline.feed_id);
                        if (articlesFeed != null)
                        {
                            icon.Source = articlesFeed.icon;
                        }
                    }*/
                    UpdateLocalizedApplicationBar(article);
                }
                e.Item.UpdateLayout();
                SetProgressBar(false);
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
                            item.Headline.unread = false;
                            UpdateLocalizedApplicationBar(article);
                            await tsk;
                        }
                    }
                    catch (TtRssException ex)
                    {
                        checkException(ex);
                    }
                }
                if (_selectedIndex <= ArticlesCollection.Count - 1 && _selectedIndex > ArticlesCollection.Count - 3)
                {
                    await LoadMoreHeadlines();
                }
            }
            catch (TtRssException ex)
            {
                SetProgressBar(false);
                checkException(ex);
            }
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
                Logger.WriteLine("WebBrowser not found");
            }
        }

        protected override async void updateCount(bool force)
        {
            int actual = _selectedIndex + 1;
            if (_IsSpecial() || _showUnreadOnly)
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

        private void UpdateLocalizedApplicationBar(Article article)
        {
            if (article.unread)
            {
                toogleReadAppBarButton.IsChecked = true;
                toogleReadAppBarButton.Label = loader.GetString("MarkReadAppBarButtonText");
            }
            else
            {
                toogleReadAppBarButton.IsChecked = false;
                toogleReadAppBarButton.Label = loader.GetString("MarkUnreadAppBarButtonText");
            }

            if (!article.marked)
            {
                toggleStarAppBarButton.IsChecked = false;
                toggleStarAppBarButton.Label = loader.GetString("StarAppBarButtonText");
            }
            else
            {
                toggleStarAppBarButton.IsChecked = true;
                toggleStarAppBarButton.Label = loader.GetString("UnStarAppBarButtonText");
            }
        }

        private void BuildLocalizedApplicationBar()
        {
            showUnreadOnlyAppBarMenu.Label = _showUnreadOnly ? loader.GetString("ShowAllArticles") : loader.GetString("ShowOnlyUnreadArticles");

            List<string> options = getSortOptions();
            sort1AppBarMenu.Label = options[0];
            sort2AppBarMenu.Label = options[1];
        }

        private async void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateField field;
            int selectedIndex = _selectedIndex;
            Article current = await ArticlesCollection[selectedIndex].getContent();
            if (sender == publishAppBarMenu)
            {
                field = UpdateField.Published;
            }
            else if (sender == toggleStarAppBarButton)
            {
                field = UpdateField.Starred;
            }
            else if (sender == toogleReadAppBarButton)
            {
                field = UpdateField.Unread;
            }
            else if (sender == markAllReadMenu)
            {
                SetProgressBar(true);
                try
                {
                    bool success = await TtRssInterface.getInterface().markAllArticlesRead(ConnectionSettings.getInstance().selectedFeed);
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
                        if (_selectedIndex == selectedIndex)
                        {
                            UpdateLocalizedApplicationBar(current);
                        }
                    }
                    Task tsk = PushNotificationHelper.UpdateLiveTile(-1);
                    SetProgressBar(false);
                    await tsk;
                }
                catch (TtRssException ex)
                {
                    checkException(ex);
                }
                return;
            }
            else if (sender == showUnreadOnlyAppBarMenu)
            {
                _showUnreadOnly = !_showUnreadOnly;
                Logger.WriteLine("ArticlePage: showUnreadOnly changed = " + _showUnreadOnly);
                showUnreadOnlyAppBarMenu.Label = _showUnreadOnly ? loader.GetString("ShowAllArticles") : loader.GetString("ShowOnlyUnreadArticles");
                await LoadHeadlines();
                _lastPivotIndex = -1;
                if (PivotControl.SelectedIndex == 0)
                {
                    _selectedIndex = 0;
                    PivotControl_LoadingPivotItem(null, new PivotItemEventArgs());
                }
                else
                {
                    _selectedIndex = 0;
                    PivotControl.SelectedIndex = 0; // go back to first pivotItem
                }
                updateCount(_showUnreadOnly);
                return;
            }
            else if (sender == sort1AppBarMenu || sender == sort2AppBarMenu)
            {
                if (_sortOrder == 0 && sender == sort1AppBarMenu || _sortOrder == 2 && sender == sort2AppBarMenu)
                {
                    _sortOrder = 1;
                }
                else if (_sortOrder != 0 && sender == sort1AppBarMenu)
                {
                    _sortOrder = 0;
                }
                else if (_sortOrder != 2 && sender == sort2AppBarMenu)
                {
                    _sortOrder = 2;
                }
                Logger.WriteLine("ArticlePage: sortOrder changed = " + _sortOrder);
                List<string> options = getSortOptions();
                sort1AppBarMenu.Label = options[0];
                sort2AppBarMenu.Label = options[1];
                await LoadHeadlines();
                _lastPivotIndex = -1;
                if (PivotControl.SelectedIndex == 0)
                {
                    _selectedIndex = 0;
                    PivotControl_LoadingPivotItem(null, new PivotItemEventArgs());
                }
                else
                {
                    _selectedIndex = 0;
                    PivotControl.SelectedIndex = 0; // go back to first pivotItem
                }
                updateCount(_showUnreadOnly);
                return;
            }
            else
            {
                return;
            }
            try
            {
                SetProgressBar(true);
                List<int> idList = new List<int>();
                idList.Add(current.id);
                bool success = await TtRssInterface.getInterface().updateArticles(idList, field, UpdateMode.Toggle);
                if (success)
                {
                    ArticlesCollection[selectedIndex].Article = await TtRssInterface.getInterface().getArticle(current.id, true);
                    if (selectedIndex == _selectedIndex)
                    {
                        UpdateLocalizedApplicationBar(ArticlesCollection[selectedIndex].Article);
                    }
                    if (sender == toogleReadAppBarButton)
                    {
                        await PushNotificationHelper.UpdateLiveTile(-1);
                    }
                }
                SetProgressBar(false);
            }
            catch (TtRssException ex)
            {
                checkException(ex);
            }
        }

        protected override async void SetProgressBar(bool on, bool setText)
        {
            if (_moreArticlesLoading && !on)
            {
                return;
            }
            ApplicationViewOrientation orientation = ApplicationView.GetForCurrentView().Orientation;
            if (on)
                {
                    ProgressBar.IsActive = true;
                }
                else
                {
                    ProgressBar.IsActive = false;
                }
                if (setText)
                {
                ProgressBarText.Text = loader.GetString("LoadMoreArticles");
                }
                else
                {
                ProgressBarText.Text = "";
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
                Logger.WriteLine("WebBrowser not found");
            }
        }

        protected override int getSelectedIdx()
        {
            return _selectedIndex;
        }

        private void Icon_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            Image img = sender as Image;
            if(img!=null)
            {
                img.Visibility = Visibility.Collapsed;
            }
        }
    }
}
