using System;
using System.Collections.Generic;
using System.Linq;
using TinyTinyRSS.Interface;
using System.Threading.Tasks;
using TinyTinyRSS.Interface.Classes;
using TinyTinyRSS.Classes;
using CaledosLab.Portable.Logging;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.UI.ViewManagement;
using Windows.UI.Popups;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml.Navigation;
using Windows.Graphics.Display;
using Windows.UI.Xaml.Controls.Primitives;

namespace TinyTinyRSS
{

    public partial class MainPage : Page
    {
        private static int pivotIdx = 0;

        private bool validConnection = false;
        private StatusBar statusBar;
        private ResourceLoader loader = new Windows.ApplicationModel.Resources.ResourceLoader();
        private bool feedListUpdate = false;
        public MainPage()
        {
            InitializeComponent();
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;
            statusBar = Windows.UI.ViewManagement.StatusBar.GetForCurrentView();
            MainPivot.SelectedIndex = pivotIdx;
            this.Loaded += PageLoaded;
        }

        private async void PageLoaded(object sender, RoutedEventArgs e)
        {
                      
            try
            {
                statusBar.ProgressIndicator.ProgressValue = null; 
                statusBar.ProgressIndicator.Text = loader.GetString("LoginProgress");
                await statusBar.ProgressIndicator.ShowAsync();
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
            statusBar.ProgressIndicator.Text = "";
            await statusBar.ProgressIndicator.HideAsync();
        }

        private void CommandInvokedHandler(IUICommand command)
        {
            if (command.Id.Equals("close"))
            {
                ConnectionSettings.getInstance().firstStart = false;
            }            
        }

        /// <summary>
        /// Load next page after a tile has been touched.
        /// </summary>
        /// <param name="sender">Button that has been touched</param>
        /// <param name="e">Events</param>
        private async void Button_Click(object sender, RoutedEventArgs e)
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
                if (sender == Fresh.Parent)
                {
                    Frame.Navigate(target, (int)FeedId.Fresh);
                }
                else if (sender == Archived.Parent)
                {
                    Frame.Navigate(target, (int)FeedId.Archived);
                }
                else if (sender == Starred.Parent)
                {
                    Frame.Navigate(target, (int)FeedId.Starred);
                }
                else if (sender == All.Parent)
                {
                    Frame.Navigate(target, (int)FeedId.All);
                }
                else if (sender == Published.Parent)
                {
                    Frame.Navigate(target, (int)FeedId.Published);
                }
                else if (sender == Recent.Parent)
                {
                    Frame.Navigate(target, (int)FeedId.RecentlyRead);
                }
                else
                {
                    Button button = sender as Button;
                    if (button.Name.StartsWith("Button"))
                    {
                        Frame.Navigate(target, ((Feed)button.DataContext).id);
                    }
                }
            }
            else
            {
                MessageDialog msgbox = new MessageDialog(loader.GetString("NoConnection"));
                await msgbox.ShowAsync();
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
                await statusBar.ProgressIndicator.ShowAsync();
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
                await statusBar.ProgressIndicator.HideAsync();
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
                foreach(Feed feed in theFeeds) {
                    extendedFeeds.Add(new ExtendedFeed(feed, getCategoryById(categories, feed.cat_id)));
                }
                
                var ordered =
                    from feed in extendedFeeds
                    orderby feed.cat
                    group feed by feed.cat into feedByTitle
                    select feedByTitle;

                groupedFeeds.Source = ordered;
                setFavorites();
                feedListUpdate = false;
            }
            catch (TtRssException ex)
            {
                checkException(ex);
            }
        }

        private void setFavorites()
        {
            // Set Favorites

            HashSet<string> favs = ConnectionSettings.getInstance().favFeeds;
            var rows = Math.Ceiling(favs.Count / 3.0);
            while (SpecialFeedsGrid.RowDefinitions.Count < rows + 2)
            {
                SpecialFeedsGrid.RowDefinitions.Add(new RowDefinition());
            }
            int row = 2;
            int col = 0;
            foreach (string fav in favs)
            {
                if (col > 2)
                {
                    col = 0;
                    row++;
                }
                int feedId;
                if (!int.TryParse(fav, out feedId))
                {
                    continue;
                }
                Feed feed = TtRssInterface.getInterface().getFeedById(feedId);
                if (feed == null)
                {
                    continue;
                }
                Button button;
                Button buttonByName = SpecialFeedsGrid.FindName("Button" + row + col) as Button;
                if (buttonByName == null || !(buttonByName is Button))
                {
                    button = new Button();
                    button.Name = "Button" + row + col;
                    button.Click += Button_Click;
                    object buttonStyle;
                    this.Resources.TryGetValue("ButtonStyle", out buttonStyle);
                    button.Style = buttonStyle as Style;
                    button.Height = SpecialFeedsGrid.RowDefinitions[0].ActualHeight;
                    Grid.SetColumn(button, col);
                    Grid.SetRow(button, row);
                    MenuFlyout m = new MenuFlyout();
                    MenuFlyoutItem mn = new MenuFlyoutItem();
                    mn.Text = loader.GetString("UnpinFlyout");
                    mn.DataContext = feed;
                    mn.Click += MenuFlyoutUnpin_Click;
                    m.Items.Add(mn);
                    button.Holding += TextBlock_Holding;
                    FlyoutBase.SetAttachedFlyout(button, m);
                    SpecialFeedsGrid.Children.Add(button);
                }
                else
                {
                    button = buttonByName;
                }
                button.DataContext = feed;
                TextBlock btnContent = new TextBlock();
                object textStyle;
                this.Resources.TryGetValue("ButtonTextStyle", out textStyle);
                btnContent.Style = textStyle as Style;
                Grid.SetColumn(button, col);
                btnContent.Text = feed.title + Environment.NewLine + feed.formattedUnread;
                button.Content = btnContent;
                col++;
            }
            // Remove additional lines
            UIElement toRemove;
            do
            {
                if (col > 2)
                {
                    col = 0;
                    row++;
                    if (SpecialFeedsGrid.RowDefinitions.Count > row)
                    {
                        SpecialFeedsGrid.RowDefinitions.RemoveAt(row);
                    }
                }
                toRemove = SpecialFeedsGrid.FindName("Button" + row + col) as UIElement;
                if (toRemove != null)
                {
                    SpecialFeedsGrid.Children.Remove(toRemove);
                }
                col++;
            } while (toRemove != null);

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
                    Fresh.Text = loader.GetString("FreshFeedsText") + Environment.NewLine + "(" + unread + ")";
                }
                else
                {
                    Fresh.Text = loader.GetString("FreshFeedsText");
                }
                await tsk;
                // Starred
                int starredCount = await TtRssInterface.getInterface().getCountForFeed(false, (int)FeedId.Starred);
                if (starredCount != 0)
                {
                    Starred.Text = loader.GetString("StarredFeedsText") + Environment.NewLine + "(" + starredCount + ")";
                }
                else
                {
                    Starred.Text = loader.GetString("StarredFeedsText");
                }
                // Archived
                int archCount = await TtRssInterface.getInterface().getCountForFeed(false, (int)FeedId.Archived);
                if (archCount != 0)
                {
                    Archived.Text = loader.GetString("ArchivedFeedsText") + Environment.NewLine + "(" + archCount + ")";
                }
                else
                {
                    Archived.Text = loader.GetString("ArchivedFeedsText");
                }
                // Published
                int publishedCount = await TtRssInterface.getInterface().getCountForFeed(false, (int)FeedId.Published);
                if (publishedCount != 0)
                {
                    Published.Text = loader.GetString("PublishedFeedsText") + Environment.NewLine + "(" + publishedCount + ")";
                }
                else
                {
                    Published.Text = loader.GetString("PublishedFeedsText");
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

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Logger.WriteLine("NavigatedTo MainPage.");
            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
        }

        private void TextBlock_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            if (!feedListUpdate)
            {
                ExtendedFeed selected = AllFeedsList.SelectedItem as ExtendedFeed;
                if (selected == null)
                {
                    return;
                }
                AllFeedsList.SelectedItem = null;
                e.Handled = true;
                Frame.Navigate(typeof(ArticlePage), selected.cat.id);
            }
        }

        private void MainPivot_PivotItemLoaded(Pivot sender, PivotItemEventArgs args)
        {
            pivotIdx = sender.SelectedIndex;
        }

        private void TextBlock_Holding(object sender, Windows.UI.Xaml.Input.HoldingRoutedEventArgs e)
        {
            FrameworkElement senderElement = sender as FrameworkElement;
            FlyoutBase flyoutBase = FlyoutBase.GetAttachedFlyout(senderElement);
            flyoutBase.ShowAt(senderElement);
        }

        private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem item = sender as MenuFlyoutItem;

            if (item != null && item.DataContext is ExtendedFeed)
            {
                ConnectionSettings.getInstance().addFavFeed(((ExtendedFeed)item.DataContext).feed.id.ToString());
                setFavorites();
                MainPivot.SelectedIndex = 0;
            }
        }

        private void MenuFlyoutUnpin_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem item = sender as MenuFlyoutItem;

            if (item != null && item.DataContext is Feed)
            {
                ConnectionSettings.getInstance().removeFavFeed(((Feed)item.DataContext).id.ToString());
                setFavorites();                
            }            
        }
    }
}