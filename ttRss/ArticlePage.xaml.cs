﻿using CaledosLab.Portable.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using TinyTinyRSS;
using TinyTinyRSS.Classes;
using TinyTinyRSS.Interface;
using TinyTinyRSS.Interface.Classes;
using TinyTinyRSSInterface.Classes;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Phone.UI.Input;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace ttRss
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ArticlePage : Page
    {
        private int feedId, _selectedIndex;
        private Collection<WrappedArticle> ArticlesCollection;
        private bool _showUnreadOnly, _moreArticles, _moreArticlesLoading;
        private ResourceLoader loader = new Windows.ApplicationModel.Resources.ResourceLoader();
        private StatusBar statusBar;
        private int _sortOrder, _lastPivotIndex;

        public ArticlePage()
        {
            this.Loaded += PageLoaded;
            InitializeComponent();
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait | DisplayOrientations.Landscape | DisplayOrientations.LandscapeFlipped;
            HardwareButtons.BackPressed += HardwareButtons_BackPressed;
            statusBar = Windows.UI.ViewManagement.StatusBar.GetForCurrentView();
            PivotHeader.Width = ResolutionHelper.GetWidthForOrientation(ApplicationView.GetForCurrentView().Orientation);
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

        private async void PageLoaded(object sender, RoutedEventArgs e)
        {
            await LoadHeadlines();
            PivotControl_LoadingPivotItem(null, new PivotItemEventArgs());
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            feedId = (int)e.Parameter;
            Logger.WriteLine("NavigatedTo ArticlePage for Feed " + feedId);
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
                    _selectedIndex = 0;
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
                catch (NullReferenceException)
                {
                    Logger.WriteLine("error loading content for article.");
                }
                if (article != null && _selectedIndex == localSelected)
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
                    UpdateLocalizedApplicationBar(article);
                }
                SetProgressBar(false);
                if (ConnectionSettings.getInstance().markRead && article != null && article.unread)
                {
                    List<int> idList = new List<int>();
                    idList.Add(article.id);
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
                (PivotItem)(PivotControl.ContainerFromItem(PivotControl.Items[PivotControl.SelectedIndex]));

            var wc = Helper.FindDescendantByName(myPivotItem, "WebContent") as WebView;
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
                    int ifOfFeed = feedId;
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

        private void ShareAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            DataTransferManager.ShowShareUI();
        }

        private void RegisterForShare()
        {
            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested += new TypedEventHandler<DataTransferManager,
                DataRequestedEventArgs>(this.ShareLinkHandler);
        }

        private void ShareLinkHandler(DataTransferManager sender, DataRequestedEventArgs e)
        {
            Headline head = ArticlesCollection[_selectedIndex].Headline;
            DataRequest request = e.Request;
            request.Data.Properties.Description = "Shared by tt-RSS Reader for Windows Phone.";
            request.Data.Properties.Title = head.title;
            request.Data.SetWebLink(new Uri(head.link));
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
                bool success = await TtRssInterface.getInterface().markAllArticlesRead(feedId);
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
                    PivotControl_LoadingPivotItem(null, new PivotItemEventArgs());
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
                sort1AppBarMenu.Label = options[0];
                sort2AppBarMenu.Label = options[1];
                await LoadHeadlines();
                _lastPivotIndex = -1;
                if (PivotControl.SelectedIndex == 0)
                {
                    PivotControl_LoadingPivotItem(null, new PivotItemEventArgs());
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

        private async void openExt_Click(object sender, RoutedEventArgs e)
        {
            if (sender == openExtAppBarButton)
            {
                WrappedArticle article = ArticlesCollection[_selectedIndex];
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
                if (_IsSpecial() && AppBar.SecondaryCommands.Contains(showUnreadOnlyAppBarMenu))
                {
                    AppBar.SecondaryCommands.Remove(showUnreadOnlyAppBarMenu);
                }
                ArticlesCollection.Clear();
                List<Headline> headlines = await TtRssInterface.getInterface().getHeadlines(feedId, unReadOnly, 0, _sortOrder);
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
                        Frame.Navigate(typeof(MainPage));
                    }
                }
                else
                {
                    foreach (Headline h in headlines)
                    {
                        ArticlesCollection.Add(new WrappedArticle(h));
                    }
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
            if (_moreArticles && !_moreArticlesLoading && _selectedIndex <= ArticlesCollection.Count - 1 && _selectedIndex > ArticlesCollection.Count - 3)
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

                    if (headlines.Count <= 1)
                    {
                        _moreArticles = false;
                    }
                    else
                    {
                        foreach (Headline h in headlines)
                        {
                            ArticlesCollection.Add(new WrappedArticle(h));
                        }
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

        private async void SetProgressBar(bool on, bool setText)
        {
            if (_moreArticlesLoading && !on)
            {
                return;
            }
            ApplicationViewOrientation orientation = ApplicationView.GetForCurrentView().Orientation;
            PivotHeader.Width = ResolutionHelper.GetWidthForOrientation(orientation);
            if (!orientation.Equals(ApplicationViewOrientation.Portrait))
            {
                if (on)
                {
                    MyProgressBar.IsIndeterminate = true;
                    MyProgressBar.Visibility = Visibility.Visible;
                    MyProgressBarText.Text = setText ? loader.GetString("loadmorearticles") : "";
                }
                else
                {
                    MyProgressBar.Visibility = Visibility.Collapsed;
                    MyProgressBarText.Text = "";
                }
            }
            else
            {
                if (on)
                {
                    await statusBar.ProgressIndicator.ShowAsync();
                }
                else
                {
                    await statusBar.ProgressIndicator.HideAsync();
                }
                if (setText)
                {
                    statusBar.ProgressIndicator.Text = loader.GetString("LoadMoreArticles");
                }
                else
                {
                    statusBar.ProgressIndicator.Text = "";
                }
            }
        }
#if WINDOWS_PHONE_APP
        void HardwareButtons_BackPressed(object sender, BackPressedEventArgs e)
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
#endif

        private async void checkException(TtRssException ex)
        {
            if (ex.Message.Equals(TtRssInterface.NONETWORKERROR))
            {
                MessageDialog msgbox = new MessageDialog(loader.GetString("NoConnection"));
                await msgbox.ShowAsync();
            }
        }

        private List<string> getSortOptions()
        {
            List<string> result = new List<string>();
            switch (_sortOrder)
            {
                case 1:
                    result.Add(loader.GetString("AppBarSortLabel") + loader.GetString("SettingsSortDefault"));
                    result.Add(loader.GetString("AppBarSortLabel") + loader.GetString("SettingsSortOld"));
                    break;
                case 2:
                    result.Add(loader.GetString("AppBarSortLabel") + loader.GetString("SettingsSortDefault"));
                    result.Add(loader.GetString("AppBarSortLabel") + loader.GetString("SettingsSortNew"));
                    break;
                default:
                    result.Add(loader.GetString("AppBarSortLabel") + loader.GetString("SettingsSortNew"));
                    result.Add(loader.GetString("AppBarSortLabel") + loader.GetString("SettingsSortOld"));
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
            var wc = Helper.FindDescendantByName(e.Item, "WebContent") as WebView;
            if (wc != null)
            {
                wc.NavigateToString("");
            }
            else
            {
                Logger.WriteLine("WebBrowser not found");
            }
        }

        private async void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ApplicationViewOrientation orientation = ApplicationView.GetForCurrentView().Orientation;
            PivotHeader.Width = ResolutionHelper.GetWidthForOrientation(orientation);
            if (!orientation.Equals(ApplicationViewOrientation.Portrait))
            {
                await statusBar.HideAsync();
                PivotHeader.Margin = new Thickness(0, 0, 0, 5);
                MyProgressBar.Visibility = Visibility.Visible;
                MyProgressBarText.Visibility = Visibility.Visible;
            }
            else
            {
                await statusBar.ShowAsync();
                PivotHeader.Margin = new Thickness(0, -20, 0, 5);
                MyProgressBar.Visibility = Visibility.Collapsed;
                MyProgressBarText.Visibility = Visibility.Collapsed;
            }
        }
    }
}