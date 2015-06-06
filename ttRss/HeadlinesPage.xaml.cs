using CaledosLab.Portable.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using TinyTinyRSS;
using TinyTinyRSS.Classes;
using TinyTinyRSS.Interface;
using TinyTinyRSS.Interface.Classes;
using TinyTinyRSSInterface.Classes;
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

namespace TinyTinyRSS
{
    public sealed partial class HeadlinesPage : AbstractArticlePage
    {
        private StatusBar statusBar;
        private int initialIndex = 0;

        public HeadlinesPage()
        {
            this.Loaded += PageLoaded;
            InitializeComponent();
            Windows.Graphics.Display.DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;
            statusBar = Windows.UI.ViewManagement.StatusBar.GetForCurrentView();
            ArticlesCollection = new ObservableCollection<WrappedArticle>();
            _showUnreadOnly = ConnectionSettings.getInstance().showUnreadOnly;
            _sortOrder = ConnectionSettings.getInstance().sortOrder;
            _moreArticles = true;
            _moreArticlesLoading = false;
            RegisterForShare();
            BuildLocalizedApplicationBar();
            UpdateLocalizedApplicationBar(true);
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
                HeadlinesView.DataContext = ArticlesCollection;
            }
            if (feedId<=0)
            {
                switch (feedId)
                {
                    case -3: FeedTitle.Text = loader.GetString("FreshFeedsText"); break;
                    case -1: FeedTitle.Text = loader.GetString("StarredFeedsText"); break;
                    case -2: FeedTitle.Text = loader.GetString("PublishedFeedsText"); break;
                    case -6: FeedTitle.Text = loader.GetString("RecentlyReadFeedText"); break;
                    case -4: FeedTitle.Text = loader.GetString("AllFeedsTitleText"); break;
                    case 0: FeedTitle.Text = loader.GetString("ArchivedFeedsText"); break;
                }
            }
            else
            {
                FeedTitle.Text = TtRssInterface.getInterface().getFeedById(feedId).title;
            }
            var sv = (ScrollViewer)VisualTreeHelper.GetChild(VisualTreeHelper.GetChild(this.HeadlinesView, 0), 0);
            sv.ViewChanged += ViewChanged;
            HeadlinesView.ScrollIntoView(ArticlesCollection[initialIndex], ScrollIntoViewAlignment.Leading);
        }

        private async void ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            if (e.IsIntermediate)
            {
                return;
            }
            ScrollViewer sv = (ScrollViewer)sender;
            var verticalOffsetValue = sv.VerticalOffset;
            var maxVerticalOffsetValue = sv.ExtentHeight - sv.ViewportHeight;
            if (verticalOffsetValue >= 0.95 * maxVerticalOffsetValue)
            {
                await LoadMoreHeadlines();
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is NavigationObject)
            {
                initialized = true;
                // Fix Backstack
                if (Frame.BackStack.Count > 2)
                {
                    Frame.BackStack.RemoveAt(Frame.BackStack.Count - 1);
                    Frame.BackStack.RemoveAt(Frame.BackStack.Count - 1);
                }
                NavigationObject nav = e.Parameter as NavigationObject;
                feedId = nav.feedId;
                _sortOrder = nav._sortOrder;
                _showUnreadOnly = nav._showUnreadOnly;
                ArticlesCollection = nav.ArticlesCollection;
                HeadlinesView.DataContext = ArticlesCollection;
                initialIndex = nav.selectedIndex;
                UpdateLocalizedApplicationBar(true);
                Logger.WriteLine("NavigatedTo HeadlinesPage from ArticlePage for Feed " + feedId);
            }
            else
            {
                initialized = false;
                feedId = (int)e.Parameter;
                Logger.WriteLine("NavigatedTo HeadlinesPage for Feed " + feedId);
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
        }

        private void MultiSelectAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            if (MultiSelectAppBarButton.IsChecked == true)
            {
                if (HeadlinesView.SelectedItem != null)
                {
                    UpdateLocalizedApplicationBar(false);
                }
                else
                {
                    UpdateLocalizedApplicationBar(true);
                }
                HeadlinesView.SelectionMode = ListViewSelectionMode.Multiple;
            }
            else
            {
                HeadlinesView.SelectedItems.Clear();
                HeadlinesView.SelectionMode = ListViewSelectionMode.Single;
                UpdateLocalizedApplicationBar(true);
            }
        }

        protected override void updateCount(bool p)
        {
            //nothing.
        }
        protected override int getSelectedIdx()
        {
            return HeadlinesView.SelectedIndex;
        }

        protected override async void SetProgressBar(bool on, bool showText)
        {
            if (_moreArticlesLoading && !on)
            {
                return;
            }
            if (on)
            {
                await statusBar.ProgressIndicator.ShowAsync();
            }
            else
            {
                await statusBar.ProgressIndicator.HideAsync();
            }
            if (showText)
            {
                statusBar.ProgressIndicator.Text = loader.GetString("LoadMoreArticles");
            }
            else
            {
                statusBar.ProgressIndicator.Text = "";
            }
        }

        private void BuildLocalizedApplicationBar()
        {
            showUnreadOnlyAppBarMenu.Label = _showUnreadOnly ? loader.GetString("ShowAllArticles") : loader.GetString("ShowOnlyUnreadArticles");

            List<string> options = getSortOptions();
            sort1AppBarMenu.Label = options[0];
            sort2AppBarMenu.Label = options[1];
            toggleStarAppBarButton.Label = loader.GetString("ToggleStarAppBarButtonText");
            toogleReadAppBarButton.Label = loader.GetString("ToggleUnreadAppBarButtonText");
        }

        private void UpdateLocalizedApplicationBar(bool hide)
        {
            toggleStarAppBarButton.Visibility = hide ? Visibility.Collapsed : Visibility.Visible;
            toogleReadAppBarButton.Visibility = hide ? Visibility.Collapsed : Visibility.Visible;
            publishAppBarMenu.Visibility = hide ? Visibility.Collapsed : Visibility.Visible;            
        }

        private async void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateField field;
            LinkedList<WrappedArticle> selectedArticles = new LinkedList<WrappedArticle>();
            if (HeadlinesView.SelectionMode == ListViewSelectionMode.Single)
            {
                if (HeadlinesView.SelectedItem == null)
                {
                    return;
                }
                selectedArticles.AddLast((WrappedArticle)HeadlinesView.SelectedItem);
            }
            else
            {
                foreach (WrappedArticle sel in HeadlinesView.SelectedItems)
                {
                    selectedArticles.AddLast(sel);
                }
            }
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
            else
            {
                return;
            }
            try
            {
                SetProgressBar(true);
                List<int> idList = new List<int>();
                foreach (WrappedArticle sel in selectedArticles)
                {
                    if (sel.Article == null)
                    {
                        await sel.getContent();
                    }
                    idList.Add(sel.Article.id);
                }
                bool success = await TtRssInterface.getInterface().updateArticles(idList, field, UpdateMode.Toggle);
                if (success)
                {
                    foreach (WrappedArticle sel in selectedArticles)
                    {
                        int idx = ArticlesCollection.IndexOf(sel);
                        ArticlesCollection[idx].Article = await TtRssInterface.getInterface().getArticle(sel.Article.id, true);

                        if (sender == toogleReadAppBarButton)
                        {
                            sel.Headline.unread = !sel.Headline.unread;
                        }
                    }
                    if (HeadlinesView.SelectionMode == ListViewSelectionMode.Multiple)
                    {
                        UpdateLocalizedApplicationBar(false);
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



        private async void AppBarButton_Click2(object sender, RoutedEventArgs e)
        {
            if (sender == markAllReadMenu)
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
                if (HeadlinesView.Items.Count > 0)
                {
                    HeadlinesView.ScrollIntoView(ArticlesCollection[0]);
                }
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
                if (HeadlinesView.Items.Count > 0)
                {
                    HeadlinesView.ScrollIntoView(ArticlesCollection[0]);
                }
                return;
            }
            else
            {
                return;
            }
        }

        private void HeadlinesView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (HeadlinesView.SelectionMode == ListViewSelectionMode.Single)
            {
                NavigationObject parameter = new NavigationObject();
                parameter.selectedIndex = HeadlinesView.SelectedIndex;
                parameter.feedId = feedId;
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
                if (HeadlinesView.SelectedItems == null || HeadlinesView.SelectedItems.Count == 0)
                {
                    UpdateLocalizedApplicationBar(true);
                    return;
                }
                else
                {
                    UpdateLocalizedApplicationBar(false);
                    return;
                }
            }
        }
    }
}
