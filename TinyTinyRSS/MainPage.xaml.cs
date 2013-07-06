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

namespace TinyTinyRSS
{
    public partial class MainPage : PhoneApplicationPage
    {
        ApplicationBarIconButton settingsAppBarButton;
        ApplicationBarIconButton refreshAppBarButton;

        ObservableCollection<Feed> feedList;

        private bool validConnection = false;

        public MainPage()
        {
            InitializeComponent();
            BuildLocalizedApplicationBar();
            feedList = new ObservableCollection<Feed>();
            AllFeedsList.DataContext = feedList;
            this.Loaded += PageLoaded;
        }

        private async void PageLoaded(object sender, RoutedEventArgs e)
        {
            validConnection = await TtRssInterface.getInterface().CheckLogin();
            if (validConnection)
            {
                // Counters
                // Unread
                int unread = await TtRssInterface.getInterface().getUnReadCount(); ;
                Fresh.Text = AppResources.FreshFeeds + " (" + unread + ")";
                // Starred
                List<Headline> headlinesS = await TtRssInterface.getInterface().getHeadlines((int) FeedId.Starred);
                Starred.Text = AppResources.StarredFeeds + " (" + headlinesS.Count + ")";
                // Archived
                List<Headline> headlinesA = await TtRssInterface.getInterface().getHeadlines((int) FeedId.Archived);
                Archived.Text = AppResources.ArchivedFeeds + " (" + headlinesA.Count + ")";
                // Published
                List<Headline> headlinesP = await TtRssInterface.getInterface().getHeadlines((int) FeedId.Published);
                Published.Text = AppResources.PublishedFeeds + " (" + headlinesP.Count + ")";
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
        private void AppBarButton_Click(object sender, EventArgs e)
        {
            if (sender.Equals(this.settingsAppBarButton))
            {
                NavigationService.Navigate(new Uri("/SettingsPage.xaml", UriKind.Relative));
            }
            else if (sender.Equals(this.refreshAppBarButton))
            {
                
            }
        }

        private async void Pivot_LoadingPivotItem(object sender, PivotItemEventArgs e)
        {
            if (e.Item == AllFeedsPivot && feedList.Count==0)
            {
                SystemTray.ProgressIndicator.IsIndeterminate = true;
                List<Feed> theFeeds = await TtRssInterface.getInterface().getFeeds();
                theFeeds.ForEach(x => feedList.Add(x));
                SystemTray.ProgressIndicator.IsIndeterminate = false;
            }
        }

        private void AllFeedsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int selectedIndex = AllFeedsList.SelectedIndex;
            if (selectedIndex == -1)
            {
                return;
            }
            Feed selected = feedList[selectedIndex];
            AllFeedsList.SelectedItem = null;
            NavigationService.Navigate(new Uri("/ArticlePage.xaml?feed=" + selected.id, UriKind.Relative));
        }
    }
}