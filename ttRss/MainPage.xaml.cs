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
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Input;
using Windows.UI.ViewManagement;
using Windows.Foundation.Metadata;

namespace TinyTinyRSS
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : AbstractArticlePage
    {
        private bool validConnection = false;
        private bool feedListUpdate = false;
        private int initialIndex;
        private List<SpecialFeed> SpecialFeedCollection;
        private List<ExtendedFeed> extendedFeeds = new List<ExtendedFeed>();
        private Point swipeStart;
        public Rect TogglePaneButtonRect
        {
            get;
            private set;
        }

        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += PageLoaded;
            ArticlesCollection = new ObservableCollection<WrappedArticle>();
            // init
            SpecialFeedCollection = new List<SpecialFeed>();
            // set datacontext
            SpecialFeedCollection.Add(new SpecialFeed(loader.GetString("AllFeedsSpecialFeedText"), "Bullets", (int)FeedId.All));
            SpecialFeedCollection.Add(new SpecialFeed(loader.GetString("FreshFeedsText"), "NewFolder", (int)FeedId.Fresh));
            SpecialFeedCollection.Add(new SpecialFeed(loader.GetString("StarredFeedsText"), "OutlineStar", (int)FeedId.Starred));
            SpecialFeedCollection.Add(new SpecialFeed(loader.GetString("PublishedFeedsText"), "World", (int)FeedId.Published));
            SpecialFeedCollection.Add(new SpecialFeed(loader.GetString("ArchivedFeedsText"), "Library", (int)FeedId.Archived));
            SpecialFeedCollection.Add(new SpecialFeed(loader.GetString("RecentlyReadFeedText"), "SyncFolder", (int)FeedId.RecentlyRead));
            SpecialFeedsList.DataContext = SpecialFeedCollection;
            Splitview_Content.ManipulationStarted += Splitview_Content_ManipulationStarted;
            Splitview_Content.ManipulationCompleted += Splitview_Content_ManipulationCompleted;
            _showUnreadOnly = ConnectionSettings.getInstance().showUnreadOnly;
            if (_showUnreadOnly)
            {
                FilterShowUnread.IsChecked = true;
            }
            else {
                FilterShowAll.IsChecked = true;
            }
            _sortOrder = ConnectionSettings.getInstance().sortOrder;
            if (_sortOrder == 0)
            {
                SortButtonDefault.IsChecked = true;
            }
            else if (_sortOrder == 1)
            {
                SortButtonNew.IsChecked = true;
            }
            else if (_sortOrder == 2)
            {
                SortButtonOld.IsChecked = true;
            }
            _moreArticles = true;
            _moreArticlesLoading = false;
            RegisterForShare();
        }

        private async void PageLoaded(object sender, RoutedEventArgs e)
        {
            // If we have a phone contract, hide the status bar
            if (ApiInformation.IsApiContractPresent("Windows.Phone.PhoneContract", 1, 0))
            {
                var statusBar = StatusBar.GetForCurrentView();
                await statusBar.HideAsync();
            }
            Frame rootFrame = Window.Current.Content as Frame;
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
                    rootFrame.CanGoBack ?
                        AppViewBackButtonVisibility.Visible :
                        AppViewBackButtonVisibility.Collapsed;
            // goto settingspage cause server setting not set.
            if ("".Equals(ConnectionSettings.getInstance().server))
            {
                Frame.Navigate(typeof(SettingsPage));
            }
            try
            {
                validConnection = await TtRssInterface.getInterface().CheckLogin();
                if (validConnection)
                {
                    await TtRssInterface.getInterface().getCounters();
                    Task specialFeedsTask = UpdateSpecialFeeds();
                    Task allFeedsTask = UpdateAllFeedsList(false);
                    Task<bool> headlinesTask = null;
                    bool titleSet = setFeedTitle();
                    if (!initialized)
                    {
                        headlinesTask = LoadHeadlines();
                        initialIndex = 0;
                    }
                    await specialFeedsTask;
                    await allFeedsTask;
                    await PushNotificationHelper.UpdateNotificationChannel();
                    if (!titleSet)
                    {
                        setFeedTitle();
                    }
                    if (!initialized)
                    {
                        bool result = await headlinesTask;
                        if (!result)
                        {
                            return;
                        }
                        HeadlinesView.DataContext = ArticlesCollection;
                    }
                    var sv = (ScrollViewer)VisualTreeHelper.GetChild(VisualTreeHelper.GetChild(this.HeadlinesView, 0), 0);
                    sv.ViewChanged += ViewChanged;
                    HeadlinesView.ScrollIntoView(ArticlesCollection[initialIndex], ScrollIntoViewAlignment.Leading);
                }
                else
                {
                    MessageDialog msgbox = new MessageDialog(loader.GetString("NoConnection"));
                    await msgbox.ShowAsync();
                }

            }
            catch (TtRssException ex)
            {
                checkException(ex);
            }
        }

        /// <summary>
        /// Set Title of feed when selected feed is changed.
        /// </summary>
        private bool setFeedTitle()
        {
            try
            {
                if (ConnectionSettings.getInstance().selectedFeed <= 0)
                {
                    switch (ConnectionSettings.getInstance().selectedFeed)
                    {
                        case -3: FeedTitle.Text = loader.GetString("FreshFeedsText"); break;
                        case -1: FeedTitle.Text = loader.GetString("StarredFeedsText"); break;
                        case -2: FeedTitle.Text = loader.GetString("PublishedFeedsText"); break;
                        case -6: FeedTitle.Text = loader.GetString("RecentlyReadFeedText"); break;
                        case -4: FeedTitle.Text = loader.GetString("AllFeedsTitleText"); break;
                        case 0: FeedTitle.Text = loader.GetString("ArchivedFeedsText"); break;
                        default: FeedTitle.Text = ""; break;
                    }
                }
                else if (ConnectionSettings.getInstance().isCategory)
                {
                    foreach (ExtendedFeed ef in extendedFeeds)
                    {
                        if (ef.cat.id == ConnectionSettings.getInstance().selectedFeed)
                        {
                            FeedTitle.Text = ef.cat.title;
                            break;
                        }
                    }
                }
                else
                {
                    try
                    {
                        FeedTitle.Text = TtRssInterface.getInterface().getFeedById(ConnectionSettings.getInstance().selectedFeed).title;
                    }
                    catch (TtRssException ex)
                    {
                        checkException(ex);
                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Callback when the SplitView's Pane is toggled open or close.  When the Pane is not visible
        /// then the floating hamburger may be occluding other content in the app unless it is aware.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TogglePaneButton_Checked(object sender, RoutedEventArgs e)
        {
            this.CheckTogglePaneButtonSizeChanged();
            if (RootSplitView.IsPaneOpen)
            {
                FeedTitle.Padding = new Thickness(8, 0, 0, 0);
            }
            else
            {
                FeedTitle.Padding = new Thickness(48, 0, 0, 0);
            }
        }

        protected override ProgressRing getProgressRing()
        {
            return ProgressBar;
        }        
        protected override ProgressBar getMarkProgressBar()
        {
            return MarkArticleProgressBar;
        }
        protected override ProgressBar getMultipleMarkProgressBar()
        {
            return MultipleMarkArticleProgressBar;
        }  
        protected override TextBlock getProgressRingText()
        {
            return ProgressBarText;
        }

        private void MultiSelectAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            if (MultiSelectAppBarButton.IsChecked == true)
            {
                if (HeadlinesView.SelectedItem != null)
                {
                    // UpdateLocalizedApplicationBar(false);
                }
                else
                {
                    // UpdateLocalizedApplicationBar(true);
                }
                closeArticleGrid();
                HeadlinesView.SelectionMode = ListViewSelectionMode.Multiple;
            }
            else
            {
                HeadlinesView.SelectedItems.Clear();
                HeadlinesView.SelectionMode = ListViewSelectionMode.Single;
                // UpdateLocalizedApplicationBar(true);
            }
        }

        /// <summary>
        /// Check for the conditions where the navigation pane does not occupy the space under the floating
        /// hamburger button and trigger the event.
        /// </summary>
        private void CheckTogglePaneButtonSizeChanged()
        {
            if (this.RootSplitView.DisplayMode == SplitViewDisplayMode.Inline ||
                this.RootSplitView.DisplayMode == SplitViewDisplayMode.Overlay)
            {
                var transform = this.TogglePaneButton.TransformToVisual(this);
                var rect = transform.TransformBounds(new Rect(0, 0, this.TogglePaneButton.ActualWidth, this.TogglePaneButton.ActualHeight));
                this.TogglePaneButtonRect = rect;
            }
            else
            {
                this.TogglePaneButtonRect = new Rect();
            }
        }

        private Category getCategoryById(List<Category> categories, int id)
        {
            foreach (Category cat in categories)
            {
                if (cat.id == id)
                {
                    return cat;
                }
            }
            return null;
        }

        private async void AllFeedsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!feedListUpdate)
            {
                ExtendedFeed selected = AllFeedsList.SelectedItem as ExtendedFeed;
                if (selected == null)
                {
                    return;
                }
                ConnectionSettings.getInstance().selectedFeed = selected.feed.id;
                SpecialFeedsList.SelectedItem = null;
                ConnectionSettings.getInstance().isCategory = false;
                await feedSelectionChanged();
            }
        }

        private async Task feedSelectionChanged()
        {
            _showUnreadOnly = ConnectionSettings.getInstance().showUnreadOnly;
            _sortOrder = ConnectionSettings.getInstance().sortOrder;
            _moreArticles = true;
            Task updateFeedCounters = UpdateFeedCounters();
            setFeedTitle();
            if (RootSplitView.DisplayMode == SplitViewDisplayMode.Overlay)
            {
                RootSplitView.IsPaneOpen = false;
            }
            if (await LoadHeadlines())
            {
                HeadlinesView.DataContext = ArticlesCollection;
            }
            MultiSelectAppBarButton.IsChecked = false;
            HeadlinesView.SelectionMode = ListViewSelectionMode.Single;
            closeArticleGrid();
            await updateFeedCounters;
        }

        /// <summary>
        /// Update list of feeds in pane.
        /// </summary>
        private async Task UpdateAllFeedsList(bool refresh)
        {
            try
            {
                SetProgressBar(true, ProgressMsg.LoginProgress);
                feedListUpdate = true;
                List<Feed> theFeeds = await TtRssInterface.getInterface().getFeeds(refresh);
                theFeeds.Sort();
                List<Category> categories = await TtRssInterface.getInterface().getCategories();

                extendedFeeds.Clear();
                foreach (Feed feed in theFeeds)
                {
                    extendedFeeds.Add(new ExtendedFeed(feed, getCategoryById(categories, feed.cat_id)));
                }

                var ordered =
                    from feed in extendedFeeds
                    orderby feed.cat
                    group feed by feed.cat into feedByTitle
                    select feedByTitle;
                groupedFeeds.Source = ordered;
                AllFeedsList.SelectedItem = null;
                feedListUpdate = false;
                SetProgressBar(false, ProgressMsg.LoginProgress);
            }
            catch (TtRssException ex)
            {
                checkException(ex);
                SetProgressBar(false, ProgressMsg.LoginProgress);
            }
        }

        /// <summary>
        /// Create SpecialFeedsList if null and update counters.
        /// </summary>
        private async Task UpdateSpecialFeeds()
        {
            // Counters in Liste aktualisieren
            try
            {
                // Unread
                int unread = await TtRssInterface.getInterface().getUnReadCount(true);
                Task tsk = PushNotificationHelper.UpdateLiveTile(unread);

                var obj1 = SpecialFeedCollection.FirstOrDefault(x => x.id == (int)FeedId.Fresh);
                if (obj1 != null) obj1.count = unread;
                await tsk;

                // Starred
                int starredCount = await TtRssInterface.getInterface().getCountForFeed(false, (int)FeedId.Starred);
                var obj2 = SpecialFeedCollection.FirstOrDefault(x => x.id == (int)FeedId.Starred);
                if (obj2 != null) obj2.count = starredCount;
                // Archived
                int archCount = await TtRssInterface.getInterface().getCountForFeed(false, (int)FeedId.Archived);
                var obj3 = SpecialFeedCollection.FirstOrDefault(x => x.id == (int)FeedId.Archived);
                if (obj3 != null) obj3.count = archCount;
                // Published
                int publishedCount = await TtRssInterface.getInterface().getCountForFeed(false, (int)FeedId.Published);
                var obj4 = SpecialFeedCollection.FirstOrDefault(x => x.id == (int)FeedId.Published);
                if (obj4 != null) obj4.count = publishedCount;
            }
            catch (TtRssException ex)
            {
                checkException(ex);
            }
        }

        /// <summary>
        /// Execute actions matching the touched app bar button.
        /// </summary>
        /// <param name="sender">Button that has been touched</param>
        /// <param name="e">Events</param>
        private async void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender.Equals(this.settingsAppBarButton))
            {
                Frame.Navigate(typeof(SettingsPage));
            }
            else if (sender.Equals(this.infoAppBarButton))
            {
                var uri = new Uri("https://thescientist.eu/?p=1057");
                await Windows.System.Launcher.LaunchUriAsync(uri);
            }
            else if (sender.Equals(this.refreshAppBarButton))
            {
                if (!validConnection)
                {
                    validConnection = await TtRssInterface.getInterface().CheckLogin();
                }
                if (!validConnection)
                {
                    MessageDialog msgbox = new MessageDialog(loader.GetString("NoConnection"));
                    await msgbox.ShowAsync();
                    return;
                }

                try
                {
                    await TtRssInterface.getInterface().getCounters();
                    await UpdateAllFeedsList(true);
                    await UpdateSpecialFeeds();
                }
                catch (TtRssException ex)
                {
                    checkException(ex);
                }
            }
        }

        /// <summary>
        /// Triggered when users nears bottom of Headlines list.
        /// Loads more headlines.
        /// </summary>
        private async void ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            if (e.IsIntermediate)
            {
                return;
            }
            ScrollViewer sv = (ScrollViewer)sender;
            var verticalOffsetValue = sv.VerticalOffset;
            var maxVerticalOffsetValue = sv.ExtentHeight - sv.ViewportHeight;
            if (verticalOffsetValue >= 0.85 * maxVerticalOffsetValue)
            {
                await LoadMoreHeadlines();
            }
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is NavigationObject)
            {
                initialized = true;
                Task updateCounters = UpdateFeedCounters();
                this.Frame.BackStack.Clear();
                NavigationObject nav = e.Parameter as NavigationObject;
                _sortOrder = nav._sortOrder;
                _showUnreadOnly = nav._showUnreadOnly;
                ArticlesCollection = new ObservableCollection<WrappedArticle>();
                foreach (WrappedArticle article in nav.ArticlesCollection)
                {
                    ArticlesCollection.Add(article);
                }
                HeadlinesView.DataContext = ArticlesCollection;
                initialIndex = nav.selectedIndex;
                Logger.WriteLine("NavigatedTo MainPage from ArticlePage for Feed " + ConnectionSettings.getInstance().selectedFeed);
                await updateCounters;
            }
            else
            {
                initialized = false;
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
        }

        /// <summary>
        /// Get counters for all feeds in SplitViewPane.
        /// </summary>
        protected async Task UpdateFeedCounters()
        {
            await TtRssInterface.getInterface().getCounters();
            Task sfUpdate = UpdateSpecialFeeds();
            foreach (ExtendedFeed ex in extendedFeeds)
            {
                ex.feed.unread = await TtRssInterface.getInterface().getCountForFeed(false, ex.feed.id);
                ex.cat.unread = await TtRssInterface.getInterface().getCountForCategory(false, ex.cat.id);
            }
            await sfUpdate;
        }

        protected override void updateCount(bool p)
        {
            //nothing.
        }
        protected override int getSelectedIdx()
        {
            return HeadlinesView.SelectedIndex;
        }

        /// <summary>
        /// Headline in List has been selected.
        /// Depending on single or multiselect decide what to do.
        /// Single select: display article.
        /// Multi select: nothing yet.
        /// </summary>
        private async void HeadlinesView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (HeadlinesView.SelectionMode == ListViewSelectionMode.Single)
            {
                if (HeadlinesView.SelectedItem == null)
                {
                    return;
                }
                var orientation = DisplayInformation.GetForCurrentView().CurrentOrientation;
                bool landscape = orientation == DisplayOrientations.Landscape || orientation == DisplayOrientations.LandscapeFlipped;
                double width = RootSplitView.ActualWidth;
                double height = RootSplitView.ActualHeight;
                if (landscape && width < 720 ||
                    !landscape && height < 800)
                {
                    NavigationObject parameter = new NavigationObject();
                    parameter.selectedIndex = HeadlinesView.SelectedIndex;
                    parameter._showUnreadOnly = _showUnreadOnly;
                    parameter._sortOrder = _sortOrder;
                    parameter.ArticlesCollection = new ObservableCollection<WrappedArticle>();
                    foreach (WrappedArticle article in ArticlesCollection)
                    {
                        parameter.ArticlesCollection.Add(article);
                    }
                    Frame.Navigate(typeof(ArticlePage), parameter);
                }
                else
                {
                    var _selectedIndex = HeadlinesView.SelectedIndex;
                    WrappedArticle item = ArticlesCollection[_selectedIndex];
                    await item.getContent();
                    Article_Grid.DataContext = item;

                    if (landscape)
                    {
                        Article_Grid.MinWidth = RootSplitView.ActualWidth / 2;
                        Article_Grid.MaxWidth = RootSplitView.ActualWidth / 2;
                        if (RootSplitView.ActualWidth / 2 < 600 && RootSplitView.IsPaneOpen)
                        {
                            VisualStateManager.GoToState(this, "paneclosed", true);
                        }
                    }
                    else
                    {
                        Article_Grid.MinHeight = RootSplitView.ActualHeight / 2;
                        Article_Grid.MaxHeight = RootSplitView.ActualHeight / 2;
                    }
                    WebContent.NavigateToString(item.Article.content);
                    if (await markArticleReadAutomatically(item.Article))
                    {
                        item.Headline.unread = false;
                        item.Article.unread = false;
                        UpdateCountManually((int)FeedId.Fresh, item.Headline.unread);
                        UpdateCountManually((int)item.Headline.feed_id, item.Headline.unread);
                        await UpdateFeedCounters();
                    }
                }
            }
        }

        private void Icon_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            Image img = sender as Image;
            if (img != null)
            {
                img.Visibility = Visibility.Collapsed;
            }
        }

        private async void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (validConnection)
            {
                SpecialFeed selFeed = (SpecialFeed)SpecialFeedsList.SelectedItem;
                if (selFeed == null)
                {
                    return;
                }
                ConnectionSettings.getInstance().selectedFeed = selFeed.id;
                AllFeedsList.SelectedItem = null;
                ConnectionSettings.getInstance().isCategory = false;
                await feedSelectionChanged();
            }
            else
            {
                MessageDialog msgbox = new MessageDialog(loader.GetString("NoConnection"));
                await msgbox.ShowAsync();
            }
        }

        private async void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender == FilterShowUnread)
            {
                _showUnreadOnly = true;
                FilterShowUnread.IsChecked = true;
                FilterShowAll.IsChecked = false;
            }
            else if (sender == FilterShowAll)
            {
                _showUnreadOnly = false;
                FilterShowUnread.IsChecked = false;
                FilterShowAll.IsChecked = true;
            }
            else
            {
                return;
            }
            Logger.WriteLine("ArticlePage: showUnreadOnly changed = " + _showUnreadOnly);
            closeArticleGrid();
            if (await LoadHeadlines())
            {
                HeadlinesView.DataContext = ArticlesCollection;
            }
            if (HeadlinesView.Items.Count > 0)
            {
                HeadlinesView.ScrollIntoView(ArticlesCollection[0]);
            }
        }

        private async void SortButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender == SortButtonDefault)
            {
                _sortOrder = 0;
                SortButtonDefault.IsChecked = true;
                SortButtonNew.IsChecked = false;
                SortButtonOld.IsChecked = false;
            }
            else if (sender == SortButtonNew)
            {
                _sortOrder = 1;
                SortButtonDefault.IsChecked = false;
                SortButtonNew.IsChecked = true;
                SortButtonOld.IsChecked = false;
            }
            else if (sender == SortButtonOld)
            {
                _sortOrder = 2;
                SortButtonDefault.IsChecked = false;
                SortButtonNew.IsChecked = false;
                SortButtonOld.IsChecked = true;
            }
            else
            {
                return;
            }
            Logger.WriteLine("ArticlePage: sortOrder changed = " + _sortOrder);
            closeArticleGrid();
            if (await LoadHeadlines())
            {
                HeadlinesView.DataContext = ArticlesCollection;
            }
            if (HeadlinesView.Items.Count > 0)
            {
                HeadlinesView.ScrollIntoView(ArticlesCollection[0]);
            }
        }

        private void ArticleGridClose(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            closeArticleGrid();
        }

        private void closeArticleGrid()
        {
            Article_Grid.DataContext = null;
            HeadlinesView.SelectedItem = null;
            var orientation = DisplayInformation.GetForCurrentView().CurrentOrientation;
            bool landscape = orientation == DisplayOrientations.Landscape || orientation == DisplayOrientations.LandscapeFlipped;
            if (landscape)
            {
                if (RootSplitView.ActualWidth >= 720 && !RootSplitView.IsPaneOpen)
                {
                    VisualStateManager.GoToState(this, "paneopen", true);
                }
                Article_Grid.MinWidth = 0;
                Article_Grid.MaxWidth = 0;
            }
            else
            {
                Article_Grid.MinWidth = 0;
                Article_Grid.MaxWidth = 0;
            }
        }

        private async void ArticleAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateField field;
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
                bool success = await markAllRead();
                if (success)
                {
                    Task updateFeedCounters = UpdateFeedCounters();
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
                    await updateFeedCounters;
                }
                return;
            }
            else
            {
                return;
            }
            try
            {
                int selectedIndex = HeadlinesView.SelectedIndex;
                WrappedArticle current = ArticlesCollection[selectedIndex];
                SetProgressBar(true, ProgressMsg.MarkArticle);
                List<int> idList = new List<int>();
                idList.Add(current.Headline.id);
                bool success = await TtRssInterface.getInterface().updateArticles(idList, field, UpdateMode.Toggle);
                if (success)
                {
                    switch (field)
                    {
                        case UpdateField.Published:
                            current.Headline.published = !current.Headline.published;
                            if (current.Article != null)
                            {
                                current.Article.published = !current.Article.published;
                            }
                            UpdateCountManually((int)FeedId.Published, current.Headline.published);
                            break;
                        case UpdateField.Unread:
                            current.Headline.unread = !current.Headline.unread;
                            if (current.Article != null)
                            {
                                current.Article.unread = !current.Article.unread;
                            }
                            UpdateCountManually((int)FeedId.Fresh, current.Headline.unread);
                            break;
                        case UpdateField.Starred:
                            current.Headline.marked = !current.Headline.marked;
                            if (current.Article != null)
                            {
                                current.Article.marked = !current.Article.marked;
                            }
                            UpdateCountManually((int)FeedId.Starred, current.Headline.marked);
                            break;
                    }
                    if (sender == toogleReadAppBarButton)
                    {
                        UpdateCountManually((int)current.Headline.feed_id, current.Headline.unread);
                        await PushNotificationHelper.UpdateLiveTile(-1);
                    }
                }
                SetProgressBar(false, ProgressMsg.MarkArticle);
            }
            catch (TtRssException ex)
            {
                checkException(ex);
                SetProgressBar(false, ProgressMsg.MarkArticle);
            }
        }

        private void UpdateCountManually(int feedid, bool add)
        {
            int change = 1;
            if (!add)
            {
                change = -1;
            }
            if (feedid > 0)
            {
                var listItem = extendedFeeds.FirstOrDefault(x => x.feed.id == feedid);
                if (listItem != null) listItem.feed.unread = listItem.feed.unread + change;
            }
            else {
                var listItem = SpecialFeedCollection.FirstOrDefault(x => x.id == feedid);
                if (listItem != null) listItem.count = listItem.count + change;
            }
        }

        /// <summary>
        /// AppBar Action for multiple items in HeadlinesView
        /// </summary>
        private async void HeadlinesAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateField field;
            LinkedList<WrappedArticle> selectedArticles = new LinkedList<WrappedArticle>();
            foreach (WrappedArticle sel in HeadlinesView.SelectedItems)
            {
                selectedArticles.AddLast(sel);
            }
            if (selectedArticles.Count == 0)
            {
                return;
            }
            if (sender == PublishAppBarButton)
            {
                field = UpdateField.Published;
            }
            else if (sender == StarAppBarButton)
            {
                field = UpdateField.Starred;
            }
            else if (sender == ReadAppBarButton)
            {
                field = UpdateField.Unread;
            }
            else
            {
                return;
            }
            try
            {
                SetProgressBar(true, ProgressMsg.MarkMultipleArticle);
                List<int> idList = new List<int>();
                foreach (WrappedArticle sel in selectedArticles)
                {
                    idList.Add(sel.Headline.id);
                }
                bool success = await TtRssInterface.getInterface().updateArticles(idList, field, UpdateMode.Toggle);
                if (success)
                {
                    Task updateFeedCounters = UpdateFeedCounters();
                    foreach (WrappedArticle sel in selectedArticles)
                    {
                        switch (field)
                        {
                            case UpdateField.Published:
                                sel.Headline.published = !sel.Headline.published;
                                if (sel.Article != null)
                                {
                                    sel.Article.published = !sel.Article.published;
                                }
                                break;
                            case UpdateField.Unread:
                                sel.Headline.unread = !sel.Headline.unread;
                                if (sel.Article != null)
                                {
                                    sel.Article.unread = !sel.Article.unread;
                                }
                                break;
                            case UpdateField.Starred:
                                sel.Headline.marked = !sel.Headline.marked;
                                if (sel.Article != null)
                                {
                                    sel.Article.marked = !sel.Article.marked;
                                }
                                break;
                        }
                    }
                    if (sender == toogleReadAppBarButton)
                    {
                        await PushNotificationHelper.UpdateLiveTile(-1);
                    }
                    await updateFeedCounters;
                }
                SetProgressBar(false, ProgressMsg.MarkMultipleArticle);
            }
            catch (TtRssException ex)
            {
                checkException(ex);
                SetProgressBar(false, ProgressMsg.MarkMultipleArticle);
            }
        }

        private void Splitview_Content_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            swipeStart = e.Position;
        }

        private void Splitview_Content_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            if (RootSplitView.DisplayMode == SplitViewDisplayMode.Overlay && !RootSplitView.IsPaneOpen)
            {
                Point currentpoint = e.Position;
                if (currentpoint.X - swipeStart.X >= 100)
                {
                    RootSplitView.IsPaneOpen = true;
                }
            }
        }

        private async void GroupHeaderTapped(object sender, TappedRoutedEventArgs e)
        {
            Grid x = (Grid)sender;
            var tb = (TextBlock)VisualTreeHelper.GetChild(x, 2);
            var catId = tb.Text;
            ConnectionSettings.getInstance().selectedFeed = Int32.Parse(catId);
            AllFeedsList.SelectedItem = null;
            SpecialFeedsList.SelectedItem = null;
            ConnectionSettings.getInstance().isCategory = true;
            await feedSelectionChanged();
        }
    }
}
