using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using TinyTinyRSSInterface;
using TinyTinyRSS.Interface;
using TinyTinyRSS.Interface.Classes;
using TinyTinyRSS.Classes;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Media;
using TinyTinyRSS.Resources;
using TinyTinyRSSInterface.Classes;
using Microsoft.Phone.Tasks;
using System.Windows.Media.Imaging;
using System.IO;
using System.IO.IsolatedStorage;
using Microsoft.Xna.Framework.Media;
using System.Windows.Resources;
using CaledosLab.Portable.Logging;
using System.Windows.Controls.Primitives;

namespace TinyTinyRSS
{
    public partial class ArticlePage : PhoneApplicationPage
    {
        private int feedId, _selectedIndex;
        private Collection<WrappedArticle> ArticlesCollection;
        private bool _showUnreadOnly, _moreArticles, _moreArticlesLoading;
        private ApplicationBarIconButton toogleReadAppBarButton, toggleStarAppBarButton, openExtAppBarButton;
        private ApplicationBarMenuItem publishAppBarMenu, showUnreadOnlyAppBarMenu, markAllReadMenu, sort1AppBarMenu, sort2AppBarMenu;
        private int _sortOrder, _lastPivotIndex; 

        public ArticlePage()
        {
            this.Loaded += PageLoaded;
            InitializeComponent();
            PivotHeader.Width = ResolutionHelper.GetWidthForOrientation(Orientation);
            ArticlesCollection = new ObservableCollection<WrappedArticle>();
            _showUnreadOnly = ConnectionSettings.getInstance().showUnreadOnly;
            _sortOrder = ConnectionSettings.getInstance().sortOrder;
            _moreArticles = false;
            _moreArticlesLoading = false;
            _selectedIndex = 0;
            _lastPivotIndex = -1;
            if (!ConnectionSettings.getInstance().progressAsCntr)
            {
                Scrollbar.Visibility = Visibility.Collapsed;
                Counter.Visibility = Visibility.Visible;
            }
            BuildLocalizedApplicationBar();
        }

        private async void PageLoaded(object sender, RoutedEventArgs e)
        {
            await LoadHeadlines();
            PivotControl_LoadingPivotItem(null, new PivotItemEventArgs(Item0));
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            string feed = "";
            if (NavigationContext.QueryString.TryGetValue("feed", out feed))
            {
                feedId = int.Parse(feed);
            }
            Logger.WriteLine("NavigatedTo ArticlePage for Feed " + feed);
        }

        private async void PivotControl_LoadingPivotItem(object sender, PivotItemEventArgs e)
        {
            if (ArticlesCollection.Count <= _selectedIndex)
            {
                return;
            }
            try
            {
                if (PivotControl.SelectedIndex == 0 && _lastPivotIndex == -1)
                {
                    _lastPivotIndex = 0;
                    _selectedIndex=0;
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

                Article article = await item.getContent();
                if (_selectedIndex == localSelected)
                {
                    setHtml(article.content);
                    var icon = Helper.FindDescendantByName(e.Item, "Icon") as Image;
                    if (icon != null)
                    {
                        Feed articlesFeed = TtRssInterface.getInterface().getFeedById(item.Headline.feed_id);
                        if (articlesFeed != null)
                        {
                            icon.Source = articlesFeed.icon;
                        }
                    }
                }
                UpdateLocalizedApplicationBar(article);
                SetProgressBar(false);
                if (ConnectionSettings.getInstance().markRead && article != null && article.unread)
                {
                    List<int> idList = new List<int>();
                    idList.Add(article.id);
                    bool success = await TtRssInterface.getInterface().updateArticles(idList, UpdateField.Unread, UpdateMode.False);
                    if (success)
                    {
                        article.unread = false;
                        item.Headline.unread = false;
                        UpdateLocalizedApplicationBar(article);
                    }
                }
                await LoadMoreHeadlines();
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
                (PivotItem)(PivotControl.ItemContainerGenerator.ContainerFromItem(PivotControl.Items[PivotControl.SelectedIndex]));

            var wc = Helper.FindDescendantByName(myPivotItem, "WebContent") as WebBrowser;
            if (wc != null)
            {
                wc.NavigateToString(content);
            }
            else
            {
                Logger.WriteLine("WebBrowser not found");
            }
        }

        private async void updateCount(bool force)
        {
            int actual = _selectedIndex + 1;
            if (_IsSpecial() || _showUnreadOnly)
            {
                int max;
                if (feedId == (int)FeedId.Fresh)
                {
                    max = await TtRssInterface.getInterface().getUnReadCount(force);
                }
                else
                {
                    if (feedId == (int)FeedId.RecentlyRead)
                    {
                        feedId = (int)FeedId.All;
                    }
                    max = await TtRssInterface.getInterface().getCountForFeed(force, feedId);
                }
                if (ArticlesCollection.Count > max)
                {
                    max = ArticlesCollection.Count;
                }
                Counter.Text = actual + "/" + max;
                Scrollbar.Maximum = max;
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
                toogleReadAppBarButton.IconUri = new Uri("/Assets/AppBar/mail-read.png", UriKind.Relative);
                toogleReadAppBarButton.Text = AppResources.MarkReadAppBarButtonText;
            }
            else
            {
                toogleReadAppBarButton.IconUri = new Uri("/Assets/AppBar/mail-unread.png", UriKind.Relative);
                toogleReadAppBarButton.Text = AppResources.MarkUnreadAppBarButtonText;
            }

            if (!article.marked)
            {
                toggleStarAppBarButton.IconUri = new Uri("/Assets/AppBar/star.png", UriKind.Relative);
                toggleStarAppBarButton.Text = AppResources.StarAppBarButtonText;
            }
            else
            {
                toggleStarAppBarButton.IconUri = new Uri("/Assets/AppBar/unstar.png", UriKind.Relative);
                toggleStarAppBarButton.Text = AppResources.UnStarAppBarButtonText;
            }
        }

        private void BuildLocalizedApplicationBar()
        {
             //ApplicationBar der Seite einer neuen Instanz von ApplicationBar zuweisen
            ApplicationBar = new ApplicationBar();

            toogleReadAppBarButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/mail-read.png", UriKind.Relative));
            toogleReadAppBarButton.Text = AppResources.MarkReadAppBarButtonText;
            toogleReadAppBarButton.Click += AppBarButton_Click;
            ApplicationBar.Buttons.Add(toogleReadAppBarButton);

            toggleStarAppBarButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/star.png", UriKind.Relative));
            toggleStarAppBarButton.Text = AppResources.StarAppBarButtonText;
            toggleStarAppBarButton.Click += AppBarButton_Click;
            ApplicationBar.Buttons.Add(toggleStarAppBarButton);

            openExtAppBarButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/globe.png", UriKind.Relative));
            openExtAppBarButton.Text = AppResources.OpenExtAppBarButtonText;
            openExtAppBarButton.Click += openExt_Click;
            ApplicationBar.Buttons.Add(openExtAppBarButton);

            //archiveAppBarMenu = new ApplicationBarMenuItem();
            //archiveAppBarMenu.Text = AppResources.ToogleArchiveAppBarButtonText;
            //archiveAppBarMenu.Click += AppBarButton_Click;
            //ApplicationBar.MenuItems.Add(archiveAppBarMenu);

            publishAppBarMenu = new ApplicationBarMenuItem();
            publishAppBarMenu.Text = AppResources.TooglePublishAppBarButtonText;
            publishAppBarMenu.Click += AppBarButton_Click;
            ApplicationBar.MenuItems.Add(publishAppBarMenu);

            showUnreadOnlyAppBarMenu = new ApplicationBarMenuItem();
            showUnreadOnlyAppBarMenu.Text = _showUnreadOnly ? AppResources.ShowAllArticles : AppResources.ShowOnlyUnreadArticles;
            showUnreadOnlyAppBarMenu.Click += AppBarButton_Click;
            ApplicationBar.MenuItems.Add(showUnreadOnlyAppBarMenu);

            markAllReadMenu = new ApplicationBarMenuItem();
            markAllReadMenu.Text = AppResources.MarkAllArticlesRead;
            markAllReadMenu.Click += AppBarButton_Click;
            ApplicationBar.MenuItems.Add(markAllReadMenu);

            List<string> options = getSortOptions();
            sort1AppBarMenu = new ApplicationBarMenuItem();
            sort1AppBarMenu.Text = options[0];
            sort1AppBarMenu.Click += AppBarButton_Click;
            ApplicationBar.MenuItems.Add(sort1AppBarMenu);
            sort2AppBarMenu = new ApplicationBarMenuItem();
            sort2AppBarMenu.Text = options[1];
            sort2AppBarMenu.Click += AppBarButton_Click;
            ApplicationBar.MenuItems.Add(sort2AppBarMenu);
        }

        private async void AppBarButton_Click(object sender, EventArgs e)
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
                bool success = await TtRssInterface.getInterface().markAllArticlesRead(feedId);
                if (success)
                {
                    (from a in ArticlesCollection where a.Headline.unread select a).ToList().ForEach(a => a.Headline.unread = false);
                    (from a in ArticlesCollection where a.Article != null && a.Article.unread select a).ToList().ForEach(a => a.Article.unread = false);
                    if (_selectedIndex == selectedIndex)
                    {
                        UpdateLocalizedApplicationBar(current);
                    }
                }
                SetProgressBar(false);
                return;
            }
            else if (sender == showUnreadOnlyAppBarMenu)
            {
                _showUnreadOnly = !_showUnreadOnly;
                Logger.WriteLine("ArticlePage: showUnreadOnly changed = " + _showUnreadOnly);
                showUnreadOnlyAppBarMenu.Text = _showUnreadOnly ? AppResources.ShowAllArticles : AppResources.ShowOnlyUnreadArticles;
                await LoadHeadlines();
                _lastPivotIndex = -1;
                if (PivotControl.SelectedIndex == 0)
                {
                    PivotControl_LoadingPivotItem(null, new PivotItemEventArgs(Item0));
                }
                else
                {
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
                sort1AppBarMenu.Text = options[0];
                sort2AppBarMenu.Text = options[1];
                await LoadHeadlines();
                _lastPivotIndex = -1;
                if (PivotControl.SelectedIndex == 0)
                {
                    PivotControl_LoadingPivotItem(null, new PivotItemEventArgs(Item0));
                }
                else
                {
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
                List<int> idList = new List<int>();
                idList.Add(current.id);
                bool success = await TtRssInterface.getInterface().updateArticles(idList, field, UpdateMode.Toggle);
                if (success)
                {
                    ArticlesCollection[selectedIndex].Article = await TtRssInterface.getInterface().getArticle(current.id, true);
                    UpdateLocalizedApplicationBar(ArticlesCollection[selectedIndex].Article);
                }
            }
            catch (TtRssException ex)
            {
                checkException(ex);
            }
        }

        private async void openExt_Click(object sender, EventArgs e)
        {
            if (sender == openExtAppBarButton)
            {
                WrappedArticle article = ArticlesCollection[_selectedIndex];
                if (article.Article != null)
                {
                    WebBrowserTask wbt = new WebBrowserTask();
                    wbt.Uri = new Uri(article.Article.link);
                    wbt.Show();
                }
                else
                {
                    WebBrowserTask wbt = new WebBrowserTask();
                    Article art = await article.getContent();
                    wbt.Uri = new Uri(art.link);
                    wbt.Show();
                }
            }
        }

        /// <summary>
        /// If Orientation is changed set the correct width of the counter textblock and turn on or off System Tray.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PhoneApplicationPage_OrientationChanged(object sender, OrientationChangedEventArgs e)
        {
            PivotHeader.Width = ResolutionHelper.GetWidthForOrientation(Orientation);
            if (Orientation.Equals(PageOrientation.LandscapeLeft) || Orientation.Equals(PageOrientation.LandscapeRight))
            {
                SystemTray.IsVisible = false;
                PivotHeader.Margin = new Thickness(0);
                MyProgressBar.Visibility = Visibility.Visible;
                MyProgressBarText.Visibility = Visibility.Visible;
            }
            else
            {
                SystemTray.IsVisible = true;
                PivotHeader.Margin = new Thickness(0, -20, 0, 0);
                MyProgressBar.Visibility = Visibility.Collapsed;
                MyProgressBarText.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Get's the headlines of the shown feed from tt-rss. 
        /// Depending on the settings, may only unread articles are loaded.
        /// </summary>
        /// <returns>true, cause void Tasks don't work.</returns>
        private async Task<bool> LoadHeadlines()
        {
            try
            {
                SetProgressBar(true);
                bool unReadOnly = !_IsSpecial() && _showUnreadOnly;
                if (_IsSpecial() && ApplicationBar.MenuItems.Contains(showUnreadOnlyAppBarMenu))
                {
                    ApplicationBar.MenuItems.Remove(showUnreadOnlyAppBarMenu);
                }
                ArticlesCollection.Clear();
                List<Headline> headlines = await TtRssInterface.getInterface().getHeadlines(feedId, unReadOnly, 0, _sortOrder);
                if (headlines.Count == 0)
                {
                    MessageBox.Show(AppResources.NoArticlesMessage);
                    if (NavigationService.CanGoBack)
                    {
                        NavigationService.GoBack();
                    }
                    else
                    {
                        NavigationService.Navigate(new Uri("/MainPage.xaml"));
                    }
                }
                else
                {
                    headlines.ForEach(x => ArticlesCollection.Add(new WrappedArticle(x)));
                    _moreArticles = headlines.Count == TtRssInterface.INITIALHEADLINECOUNT;
                    updateCount(false);
                }
            }
            catch (TtRssException ex)
            {
                checkException(ex);
            }
            finally
            {
                SetProgressBar(false);
            }
            return true;
        }

        /// <summary>
        /// Check if the shown feed is a special on (archived, unread, etc.)
        /// </summary>
        /// <returns>True if feedId is in between -4 and 1</returns>
        private bool _IsSpecial()
        {
            return feedId > -4 && feedId < 1;
        }

        /// <summary>
        /// When a pivot item is loaded check if you need to load more articles, cause of lazy loading.
        /// </summary>
        private async Task LoadMoreHeadlines()
        {
            if (_moreArticles && !_moreArticlesLoading && _selectedIndex <= ArticlesCollection.Count - 1 && _selectedIndex > ArticlesCollection.Count-3)
            {
                try
                {
                    _moreArticlesLoading = true;
                    SetProgressBar(true, true);
                    bool unReadOnly = !_IsSpecial() && _showUnreadOnly;
                    int skip = ArticlesCollection.Count;
                    if (feedId == (int)FeedId.Fresh || _showUnreadOnly)
                    {
                        skip = ArticlesCollection.Count(e => e.Headline.unread);
                    }
                    List<Headline> headlines = await TtRssInterface.getInterface().getHeadlines(feedId, unReadOnly, skip, _sortOrder);

                    if (headlines.Count == 0)
                    {
                        _moreArticles = false;
                    }
                    else
                    {
                        _moreArticles = headlines.Count == TtRssInterface.ADDITIONALHEADLINECOUNT;
                        headlines.ForEach(x => ArticlesCollection.Add(new WrappedArticle(x)));
                        updateCount(false);
                    }
                }
                catch (TtRssException ex)
                {
                    checkException(ex);
                }              
                finally
                {
                    _moreArticlesLoading = false;
                    SetProgressBar(false);
                }
            }
        }

        private void SetProgressBar(bool on)
        {
            SetProgressBar(on, false);
        }

        private void SetProgressBar(bool on, bool setText)
        {
            if (_moreArticlesLoading && !on)
            {
                return;
            }
            if (Orientation.Equals(PageOrientation.LandscapeLeft) || Orientation.Equals(PageOrientation.LandscapeRight))
            {
                MyProgressBar.IsIndeterminate = on;
                MyProgressBarText.Text = setText ? AppResources.LoadMoreArticles : "";
            }
            else
            {
                SystemTray.ProgressIndicator.IsIndeterminate = on;
                SystemTray.ProgressIndicator.Text = setText ? AppResources.LoadMoreArticles : "";
            }
        }

        private void PhoneApplicationPage_BackKeyPress(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!_IsSpecial() && ArticlesCollection.Count > 0)
            {
                try
                {
                    Feed theFeed = TtRssInterface.getInterface().getFeedById(feedId);
                    if (theFeed != null)
                    {
                        theFeed.unread = ArticlesCollection.Count(x => x.Headline.unread);
                    }
                }
                catch (TtRssException ex)
                {
                    checkException(ex);
                }
            }
        }

        private void checkException(TtRssException ex)
        {
            if (ex.Message.Equals(TtRssInterface.NONETWORKERROR))
            {
                MessageBox.Show(AppResources.NoConnection);
            }
        }

        private List<string> getSortOptions()
        {
            List<string> result = new List<string>();
            switch (_sortOrder)
            {
                case 1:
                    result.Add(AppResources.AppBarSortLabel + AppResources.SettingsSortDefault);
                    result.Add(AppResources.AppBarSortLabel + AppResources.SettingsSortOld);
                    break;
                case 2:
                    result.Add(AppResources.AppBarSortLabel + AppResources.SettingsSortDefault);
                    result.Add(AppResources.AppBarSortLabel + AppResources.SettingsSortNew);
                    break;
                default:
                    result.Add(AppResources.AppBarSortLabel + AppResources.SettingsSortNew);
                    result.Add(AppResources.AppBarSortLabel + AppResources.SettingsSortOld);
                    break;
            }
            return result;
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
            var wc = Helper.FindDescendantByName(e.Item, "WebContent") as WebBrowser;
            if (wc != null)
            {
                wc.NavigateToString("");
            }
            else
            {
                Logger.WriteLine("WebBrowser not found");
            }
        }
    }
}