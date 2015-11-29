using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using TinyTinyRSS.Classes;
using TinyTinyRSS.Interface;
using TinyTinyRSS.Interface.Classes;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace TinyTinyRSS
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private bool validConnection = false;
        private ResourceLoader loader = new Windows.ApplicationModel.Resources.ResourceLoader();
        private bool feedListUpdate = false;
        public Rect TogglePaneButtonRect
        {
            get;
            private set;
        }

        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += PageLoaded;
        }

        private async void PageLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                validConnection = await TtRssInterface.getInterface().CheckLogin();
                if (validConnection)
                {
                    await TtRssInterface.getInterface().getCounters();
                    await UpdateSpecialFeeds();
                    await UpdateAllFeedsList(true);
                    await PushNotificationHelper.UpdateNotificationChannel();
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
        /// Callback when the SplitView's Pane is toggled open or close.  When the Pane is not visible
        /// then the floating hamburger may be occluding other content in the app unless it is aware.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TogglePaneButton_Checked(object sender, RoutedEventArgs e)
        {
            this.CheckTogglePaneButtonSizeChanged();
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

        private async void FeedTapped(object sender, TappedRoutedEventArgs e)
        {
            Type target;
            var setting = ConnectionSettings.getInstance().headlinesView;
            if (setting == 0 || setting == 2)
            {
                target = typeof(ArticlePage);
            }
            else
            {
                target = typeof(HeadlinesPage);
            }

            if (validConnection)
            {
                if (sender == Fresh)
                {
                    Frame.Navigate(target, (int)FeedId.Fresh);
                }
                else if (sender == Archived)
                {
                    Frame.Navigate(target, (int)FeedId.Archived);
                }
                else if (sender == Starred)
                {
                    Frame.Navigate(target, (int)FeedId.Starred);
                }
                else if (sender == All)
                {
                    Frame.Navigate(target, (int)FeedId.All);
                }
                else if (sender == Published)
                {
                    Frame.Navigate(target, (int)FeedId.Published);
                }
                else if (sender == Recent)
                {
                    Frame.Navigate(target, (int)FeedId.RecentlyRead);
                }
            }
            else
            {
                MessageDialog msgbox = new MessageDialog(loader.GetString("NoConnection"));
                await msgbox.ShowAsync();
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

        private void AllFeedsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!feedListUpdate)
            {
                Type target;
                var setting = ConnectionSettings.getInstance().headlinesView;
                if (setting == 0 || setting == 1)
                {
                    target = typeof(ArticlePage);
                }
                else
                {
                    target = typeof(HeadlinesPage);
                }
                ExtendedFeed selected = AllFeedsList.SelectedItem as ExtendedFeed;
                if (selected == null)
                {
                    return;
                }
                AllFeedsList.SelectedItem = null;
                Frame.Navigate(target, selected.feed.id);
            }
        }
        private async Task UpdateAllFeedsList(bool refresh)
        {
            try
            {
                feedListUpdate = true;
                List<Feed> theFeeds = await TtRssInterface.getInterface().getFeeds(refresh);
                theFeeds.Sort();
                List<Category> categories = await TtRssInterface.getInterface().getCategories();

                List<ExtendedFeed> extendedFeeds = new List<ExtendedFeed>();
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
                feedListUpdate = false;
            }
            catch (TtRssException ex)
            {
                checkException(ex);
            }
        }
        private async Task UpdateSpecialFeeds()
        {
            // Counters
            try
            {
                // Unread
                int unread = await TtRssInterface.getInterface().getUnReadCount(true);
                Task tsk = PushNotificationHelper.UpdateLiveTile(unread);
                if (unread != 0)
                {
                    Fresh.Text = loader.GetString("FreshFeedsText") + " (" + unread + ")";
                }
                else
                {
                    Fresh.Text = loader.GetString("FreshFeedsText") + " (-)";
                }
                await tsk;
                // Starred
                int starredCount = await TtRssInterface.getInterface().getCountForFeed(false, (int)FeedId.Starred);
                if (starredCount != 0)
                {
                    Starred.Text = loader.GetString("StarredFeedsText") + " (" + starredCount + ")";
                }
                else
                {
                    Starred.Text = loader.GetString("StarredFeedsText") + " (-)";
                }
                // Archived
                int archCount = await TtRssInterface.getInterface().getCountForFeed(false, (int)FeedId.Archived);
                if (archCount != 0)
                {
                    Archived.Text = loader.GetString("ArchivedFeedsText") + " (" + archCount + ")";
                }
                else
                {
                    Archived.Text = loader.GetString("ArchivedFeedsText") + " (-)";
                }
                // Published
                int publishedCount = await TtRssInterface.getInterface().getCountForFeed(false, (int)FeedId.Published);
                if (publishedCount != 0)
                {
                    Published.Text = loader.GetString("PublishedFeedsText") + " (" + publishedCount + ")";
                }
                else
                {
                    Published.Text = loader.GetString("PublishedFeedsText") + " (-)";
                }
            }
            catch (TtRssException ex)
            {
                checkException(ex);
            }
        }

        private async void checkException(TtRssException ex)
        {
            if (ex.Message.Equals(TtRssInterface.NONETWORKERROR))
            {
                MessageDialog msgbox = new MessageDialog(loader.GetString("NoConnection"));
                await msgbox.ShowAsync();
            }
        }
    }
}
