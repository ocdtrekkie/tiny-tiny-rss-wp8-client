using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using TinyTinyRSS.Resources;
using TinyTinyRSSInterface;
using TinyTinyRSS.Interface;
using System.Threading.Tasks;
using System.Windows.Media;
using TinyTinyRSS.Interface.Classes;
using System.Collections.ObjectModel;
using TinyTinyRSSInterface.Classes;
using System.Windows.Data;
using TinyTinyRSS.Classes;
using CaledosLab.Portable.Logging;
using Microsoft.Phone.Tasks;
using System.IO.IsolatedStorage;
using System.IO;

namespace TinyTinyRSS
{
    public partial class MainPage : PhoneApplicationPage
    {
        ApplicationBarIconButton settingsAppBarButton;
        ApplicationBarIconButton refreshAppBarButton;

        List<KeyedList<string, ExtendedFeed>> FeedListDataSource;

        private bool validConnection = false;

        public MainPage()
        {
            InitializeComponent();
            BuildLocalizedApplicationBar();
            if (SystemTray.GetProgressIndicator(this) == null)
            {
                SystemTray.SetProgressIndicator(this, new ProgressIndicator());
            }
            this.Loaded += PageLoaded;
        }

        private async void PageLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                SystemTray.ProgressIndicator.IsIndeterminate = true;
                SystemTray.ProgressIndicator.Text = AppResources.LoginProgress;
                validConnection = await TtRssInterface.getInterface().CheckLogin();
                if (validConnection)
                {
                    await TtRssInterface.getInterface().getCounters();
                    await UpdateSpecialFeeds();
                    await UpdateAllFeedsList(true);
                }
                else
                {
                    MessageBox.Show(AppResources.NoConnection);
                }
                
            }
            catch (TtRssException ex)
            {
                checkException(ex);
            }
            finally
            {
                try
                {
                    SystemTray.ProgressIndicator.Text = "";
                    SystemTray.ProgressIndicator.IsIndeterminate = false;
                }
                catch (NullReferenceException nre)
                {
                    Logger.WriteLine("NRE probably cause page is not there anymore.");
                    Logger.WriteLine(nre);
                }
            }
        }

        private void BuildLocalizedApplicationBar()
        {
            // ApplicationBar der Seite einer neuen Instanz von ApplicationBar zuweisen
            ApplicationBar = new ApplicationBar();

            // Eine neue Schaltfläche erstellen und als Text die lokalisierte Zeichenfolge aus AppResources zuweisen.
            settingsAppBarButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/feature.settings.png", UriKind.Relative));
            settingsAppBarButton.Text = AppResources.SettingsAppBarButtonText;
            settingsAppBarButton.Click += AppBarButton_Click;
            ApplicationBar.Buttons.Add(settingsAppBarButton);

            refreshAppBarButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/refresh.png", UriKind.Relative));
            refreshAppBarButton.Text = AppResources.RefreshAppBarButtonText;
            refreshAppBarButton.Click += AppBarButton_Click;
            ApplicationBar.Buttons.Add(refreshAppBarButton);
        }

        /// <summary>
        /// Load next page after a tile has been touched.
        /// </summary>
        /// <param name="sender">Button that has been touched</param>
        /// <param name="e">Events</param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (validConnection)
            {
                if (sender == Fresh.Parent)
                {
                    NavigationService.Navigate(new Uri("/ArticlePage.xaml?feed=" + (int)FeedId.Fresh, UriKind.Relative));
                }
                else if (sender == Archived.Parent)
                {
                    NavigationService.Navigate(new Uri("/ArticlePage.xaml?feed=" + (int)FeedId.Archived, UriKind.Relative));
                }
                else if (sender == Starred.Parent)
                {
                    NavigationService.Navigate(new Uri("/ArticlePage.xaml?feed=" + (int)FeedId.Starred, UriKind.Relative));
                }
                else if (sender == All.Parent)
                {
                    NavigationService.Navigate(new Uri("/ArticlePage.xaml?feed=" + (int)FeedId.All, UriKind.Relative));
                }
                else if (sender == Published.Parent)
                {
                    NavigationService.Navigate(new Uri("/ArticlePage.xaml?feed=" + (int)FeedId.Published, UriKind.Relative));
                }
                else if (sender == Recent.Parent)
                {
                    NavigationService.Navigate(new Uri("/ArticlePage.xaml?feed=" + (int)FeedId.RecentlyRead, UriKind.Relative));
                }
            }
            else
            {
                MessageBox.Show(AppResources.NoConnection);
            }

        }
        /// <summary>
        /// Execute actions matching the touched app bar button.
        /// </summary>
        /// <param name="sender">Button that has been touched</param>
        /// <param name="e">Events</param>
        private async void AppBarButton_Click(object sender, EventArgs e)
        {
            if (sender.Equals(this.settingsAppBarButton))
            {
                NavigationService.Navigate(new Uri("/SettingsPage.xaml", UriKind.Relative));
            }
            else if (sender.Equals(this.refreshAppBarButton))
            {
                if (!validConnection)
                {
                    validConnection = await TtRssInterface.getInterface().CheckLogin(); 
                }                    
                SystemTray.ProgressIndicator.IsIndeterminate = true;
                try
                {
                    await TtRssInterface.getInterface().getCounters();
                    if (MainPivot.SelectedItem == AllFeedsPivot)
                    {
                        await UpdateAllFeedsList(true);
                    }
                    else
                    {
                        await UpdateSpecialFeeds();
                    }
                }
                catch (TtRssException ex)
                {
                    checkException(ex);
                }
                SystemTray.ProgressIndicator.IsIndeterminate = false;
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
            ExtendedFeed selected = AllFeedsList.SelectedItem as ExtendedFeed;
            if (selected == null)
            {
                return;
            }
            AllFeedsList.SelectedItem = null;
            NavigationService.Navigate(new Uri("/ArticlePage.xaml?feed=" + selected.feed.id, UriKind.Relative));
        }

        private async Task<bool> UpdateAllFeedsList(bool refresh)
        {
            try
            {
                List<Feed> theFeeds = await TtRssInterface.getInterface().getFeeds(refresh);
                theFeeds.Sort();
                List<Category> categories = await TtRssInterface.getInterface().getCategories();

                List<ExtendedFeed> extendedFeeds = new List<ExtendedFeed>();
                theFeeds.ForEach(x => extendedFeeds.Add(new ExtendedFeed(x, getCategoryById(categories, x.cat_id))));

                var groupedFeeds =
                    from feed in extendedFeeds
                    orderby feed.cat.combined
                    group feed by feed.cat.combined into feedByTitle
                    select new KeyedList<string, ExtendedFeed>(feedByTitle);

                FeedListDataSource = new List<KeyedList<string, ExtendedFeed>>(groupedFeeds);
                AllFeedsList.DataContext = FeedListDataSource;
            }
            catch (TtRssException ex)
            {
                checkException(ex);
            }
            return true;
        }

        private async Task<bool> UpdateSpecialFeeds()
        {
            // Counters
            // Unread
            try
            {
                int unread = await TtRssInterface.getInterface().getUnReadCount(true);
                if (unread != 0)
                {
                    Fresh.Text = AppResources.FreshFeeds + Environment.NewLine + "(" + unread + ")";
                }
                else
                {
                    Fresh.Text = AppResources.FreshFeeds;
                }
                // Starred
                int starredCount = await TtRssInterface.getInterface().getCountForFeed(false, (int)FeedId.Starred);
                if (starredCount != 0)
                {
                    Starred.Text = AppResources.StarredFeeds + Environment.NewLine + "(" + starredCount + ")";
                }
                else
                {
                    Starred.Text = AppResources.StarredFeeds;
                }
                // Archived
                int archCount = await TtRssInterface.getInterface().getCountForFeed(false, (int)FeedId.Archived);
                if (archCount != 0)
                {
                    Archived.Text = AppResources.ArchivedFeeds + Environment.NewLine + "(" + archCount + ")";
                }
                else
                {
                    Archived.Text = AppResources.ArchivedFeeds;
                }
                // Published
                int publishedCount = await TtRssInterface.getInterface().getCountForFeed(false, (int)FeedId.Published);
                if (publishedCount != 0)
                {
                    Published.Text = AppResources.PublishedFeeds + Environment.NewLine + "(" + publishedCount + ")";
                }
                else
                {
                    Published.Text = AppResources.PublishedFeeds;
                }
            }
            catch (TtRssException ex)
            {
                checkException(ex);
            }

            return true;
        }

        private void checkException(TtRssException ex)
        {
            if (ex.Message.Equals(TtRssInterface.NONETWORKERROR))
            {
                MessageBox.Show(AppResources.NoConnection);
            }
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            Logger.WriteLine("NavigatedTo MainPage.");
        }
    }
}