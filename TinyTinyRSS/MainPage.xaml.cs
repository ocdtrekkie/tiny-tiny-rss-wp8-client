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
            this.Loaded += PageLoaded;
        }

        private async void PageLoaded(object sender, RoutedEventArgs e)
        {
            validConnection = await TtRssInterface.getInterface().CheckLogin();
            if (validConnection)
            {
                await UpdateSpecialFeeds();
            }
            else
            {
                MessageBox.Show(AppResources.NoConnection);
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
                if (MainPivot.SelectedItem == AllFeedsPivot)
                {
                    await UpdateAllFeedsList();
                }
                else
                {
                    await UpdateSpecialFeeds();
                }
            }
        }

        private async void Pivot_LoadingPivotItem(object sender, PivotItemEventArgs e)
        {
            if (e.Item == AllFeedsPivot && FeedListDataSource == null)
            {
                await UpdateAllFeedsList();
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

        private async Task<bool> UpdateAllFeedsList()
        {
            SystemTray.ProgressIndicator.IsIndeterminate = true;
            List<Feed> theFeeds = await TtRssInterface.getInterface().getFeeds();
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
            SystemTray.ProgressIndicator.IsIndeterminate = false;
            return true;
        }

        private async Task<bool> UpdateSpecialFeeds()
        {
            // Counters
            // Unread
            int unread = await TtRssInterface.getInterface().getUnReadCount();
            if (unread != 0)
            {
                Fresh.Text = AppResources.FreshFeeds + Environment.NewLine + " (" + unread + ")";
            }
            // Starred
            List<Headline> headlinesS = await TtRssInterface.getInterface().getHeadlines((int)FeedId.Starred);
            if (headlinesS.Count != 0)
            {
                Starred.Text = AppResources.StarredFeeds + Environment.NewLine + " (" + headlinesS.Count + ")";
            }
            // Archived
            List<Headline> headlinesA = await TtRssInterface.getInterface().getHeadlines((int)FeedId.Archived);
            if (headlinesA.Count != 0)
            {
                Archived.Text = AppResources.ArchivedFeeds + Environment.NewLine + " (" + headlinesA.Count + ")";
            }
            // Published
            List<Headline> headlinesP = await TtRssInterface.getInterface().getHeadlines((int)FeedId.Published);
            if (headlinesP.Count != 0)
            {
                Published.Text = AppResources.PublishedFeeds + Environment.NewLine + " (" + headlinesP.Count + ")";
            }
            return true;
        }
    }
}