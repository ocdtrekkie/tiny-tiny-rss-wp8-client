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

namespace TinyTinyRSS
{
    public partial class ArticlePage : PhoneApplicationPage
    {
        private int feedId;
        private ObservableCollection<WrappedArticle> ArticlesCollection;
        private int TotalCount;
        private bool _showUnreadOnly, _moreArticles, _moreArticlesLoading;
        private ApplicationBarIconButton toogleReadAppBarButton, toggleStarAppBarButton, openExtAppBarButton;
        private ApplicationBarMenuItem publishAppBarMenu, showUnreadOnlyAppBarMenu;

        public ArticlePage()
        {
            InitializeComponent();
            PivotHeader.Width = ResolutionHelper.GetWidthForOrientation(Orientation);
            ArticlesCollection = new ObservableCollection<WrappedArticle>();
            
            _showUnreadOnly = ConnectionSettings.getInstance().showUnreadOnly;
            _moreArticles = false;
            _moreArticlesLoading = false;
            BuildLocalizedApplicationBar();
            this.Loaded += PageLoaded;
        }

        private async void PageLoaded(object sender, RoutedEventArgs e)
        {
            PivotControl.DataContext = ArticlesCollection;
            await LoadHeadlines(false);
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
            SetProgressBar(true);
            int selectedIndex = PivotControl.SelectedIndex;
            Counter.Text = Helper.AppendPlus(_moreArticles, (selectedIndex + 1) + "/" + TotalCount);
            
            WrappedArticle item = ArticlesCollection[selectedIndex];
            if (item.Article == null)
            {
                item.Article = await TtRssInterface.getInterface().getArticle(item.Headline.id);                              
            }
            setHtml(item.Article.content);
            var icon = Helper.FindDescendantByName(e.Item, "Icon") as Image;
            if (icon != null)
            {   
                Feed articlesFeed = TtRssInterface.getInterface().getFeedById(item.Headline.feed_id);
                if(articlesFeed!=null)
                {
                    icon.Source = articlesFeed.icon;
                }
            } 
            UpdateLocalizedApplicationBar(item.Article);
            SetProgressBar(false);
            if (ConnectionSettings.getInstance().markRead && item.Article != null && item.Article.unread)
            {
                bool success = await TtRssInterface.getInterface().updateArticle(item.Article.id, UpdateField.Unread, UpdateMode.False);
                if (success)
                {
                    item.Article.unread = false;
                    item.Headline.unread = false;
                    UpdateLocalizedApplicationBar(item.Article);
                }
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
            // ApplicationBar der Seite einer neuen Instanz von ApplicationBar zuweisen
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
        }

        private async void AppBarButton_Click(object sender, EventArgs e)
        {
            UpdateField field;
            Article current = ArticlesCollection[PivotControl.SelectedIndex].Article;
            bool remove = false;
            if (sender == publishAppBarMenu)
            {
                field = UpdateField.Published;
                if (feedId == (int) FeedId.Published)
                {
                    remove = true;
                }
            }            
            else if (sender == toggleStarAppBarButton)
            {
                field = UpdateField.Starred;
                if (feedId == (int)FeedId.Starred)
                {
                    remove = true;
                }
            }
            else if (sender == toogleReadAppBarButton)
            {
                field = UpdateField.Unread;
            }
            else if (sender == showUnreadOnlyAppBarMenu)
            {
                _showUnreadOnly = !_showUnreadOnly;
                showUnreadOnlyAppBarMenu.Text = _showUnreadOnly ? AppResources.ShowAllArticles : AppResources.ShowOnlyUnreadArticles;
                await LoadHeadlines(true);
                return;
            }
            else
            {
                return;
            }
            bool success = await TtRssInterface.getInterface().updateArticle(current.id, field, UpdateMode.Toggle);
            if (success)
            {
                ArticlesCollection[PivotControl.SelectedIndex].Article = await TtRssInterface.getInterface().getArticle(current.id);
                UpdateLocalizedApplicationBar(ArticlesCollection[PivotControl.SelectedIndex].Article);
            }
            if (remove)
            {
                //if (TotalCount == 1)
                //{
                //    NavigationService.GoBack();
                //}
                //else
                //{
                //    TotalCount--;
                //}
                //ArticlesCollection.RemoveAt(PivotControl.SelectedIndex);
                TtRssInterface.getInterface().removeHeadlineFromCache(feedId, ArticlesCollection[PivotControl.SelectedIndex].Headline);
            }
        }

        private void openExt_Click(object sender, EventArgs e)
        { 
            if (sender == openExtAppBarButton)
            {
                WebBrowserTask wbt = new WebBrowserTask();
                wbt.Uri = new Uri(ArticlesCollection[PivotControl.SelectedIndex].Article.link);
                wbt.Show();
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
                PivotHeader.Margin = new Thickness(0,-20,0,0);
                MyProgressBar.Visibility = Visibility.Collapsed;
                MyProgressBarText.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Get's the headlines of the shown feed from tt-rss. 
        /// Depending on the settings, may only unread articles are loaded.
        /// </summary>
        /// <returns>true, cause void Tasks don't work.</returns>
        private async Task<bool> LoadHeadlines(bool forceRefresh)
        {
            SetProgressBar(true);
            bool unReadOnly = !_IsSpecial() && _showUnreadOnly;
            if (_IsSpecial() && ApplicationBar.MenuItems.Contains(showUnreadOnlyAppBarMenu))
            {
                ApplicationBar.MenuItems.Remove(showUnreadOnlyAppBarMenu);
            }
            ArticlesCollection.Clear();
            Counter.Text = "";
            List<Headline> headlines = await TtRssInterface.getInterface().getHeadlines(forceRefresh, feedId, unReadOnly);
            if (headlines.Count == 0)
            {
                MessageBox.Show("No Articles found here.");
                NavigationService.GoBack();
            }
            else
            {
                TotalCount = headlines.Count;
                _moreArticles = TotalCount == TtRssInterface.INITIALHEADLINECOUNT;
                headlines.ForEach(x => ArticlesCollection.Add(new WrappedArticle(x)));
            }
            SetProgressBar(false);
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
        /// When a pivot item is loaded check if you need o load more articles, cause of lazy loading.
        /// </summary>
        private async void PivotControl_LoadedPivotItem(object sender, PivotItemEventArgs e)
        {
            if (_moreArticles && PivotControl.SelectedIndex <= TotalCount - 1 && PivotControl.SelectedIndex >= TotalCount - 3 && !_moreArticlesLoading)
            {
                _moreArticlesLoading = true;
                SetProgressBar(true, true);
                bool unReadOnly = !_IsSpecial() && _showUnreadOnly;
                List<Headline> headlines = await TtRssInterface.getInterface().loadMoreHeadlines(feedId, unReadOnly, TotalCount);
                if (headlines.Count == 0)
                {
                    _moreArticles = false;
                }
                else
                {
                    TotalCount = TotalCount + headlines.Count;
                    _moreArticles = headlines.Count == TtRssInterface.ADDITIONALHEADLINECOUNT;
                    headlines.ForEach(x => ArticlesCollection.Add(new WrappedArticle(x)));
                }
                _moreArticlesLoading = false;
                SetProgressBar(false);
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
                Feed theFeed = TtRssInterface.getInterface().getFeedById(feedId);
                if (theFeed != null)
                {
                    theFeed.unread = ArticlesCollection.Count(x => x.Headline.unread);
                }
            }
        }

        public bool _moreLoading { get; set; }
    }
}