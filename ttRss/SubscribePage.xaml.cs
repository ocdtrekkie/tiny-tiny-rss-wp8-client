using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Xml.Linq;
using TinyTinyRSS.Classes;
using TinyTinyRSS.Interface;
using TinyTinyRSS.Interface.Classes;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Resources;
using Windows.Foundation.Diagnostics;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace TinyTinyRSS
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SubscribePage : Page
    {
        private ResourceLoader loader = new Windows.ApplicationModel.Resources.ResourceLoader();
        private LoggingChannel channel;
        private Collection<Category> categories = new ObservableCollection<Category>();
        public SubscribePage()
        {
            InitializeComponent();
            channel = new LoggingChannel("SubscribePage.cs", null);
            LogSession.addChannel(channel);
            this.Loaded += PageLoaded;
        }

        private async void PageLoaded(object sender, RoutedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
                    rootFrame.CanGoBack ?
                        AppViewBackButtonVisibility.Visible :
                        AppViewBackButtonVisibility.Collapsed;
            List<Category> cats = await TtRssInterface.getInterface().getCategories();
            foreach (Category c in cats)
            {
                categories.Add(c);
            }
            SubscribeGroup.DataContext = categories;
            SubscribeGroup.SelectedIndex = 0;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (SubscribeUrl.Text.Length == 0)
            {
                MessageDialog msgbox = new MessageDialog(loader.GetString("FeedSubscribeEmptyUrl"));
                await msgbox.ShowAsync();
                return;
            }
            try
            {
                ProgressBar.Visibility = Visibility.Visible;
                int groupId = ((Category)SubscribeGroup.SelectedItem).id;
                string response = await TtRssInterface.getInterface().subscribeToFeed(SubscribeUrl.Text,
                    groupId, SubscribeUsername.Text, SubscribePassword.Text);
                if (response == null)
                {
                    await TtRssInterface.getInterface().getFeeds();
                    if (Frame.CanGoBack)
                    {
                        Frame.GoBack();
                    }
                    else
                    {
                        Frame.Navigate(typeof(MainPage));
                    }
                }
                else
                {
                    MessageDialog msgbox = new MessageDialog(
                        loader.GetString("FeedSubscribeFailed") + Environment.NewLine + response);
                    await msgbox.ShowAsync();
                }
            }
            catch (TtRssException ex)
            {
                if (ex.Message.Equals(TtRssInterface.NONETWORKERROR))
                {
                    MessageDialog msgbox = new MessageDialog(loader.GetString("NoConnection"));
                    await msgbox.ShowAsync();
                }
                else
                {
                    MessageDialog msgbox = new MessageDialog(ex.Message);
                    await msgbox.ShowAsync();
                }
            }
            finally
            {
                ProgressBar.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Should be used by the file activation to subscribe from feed file, but
        /// that won't work cause we need the rss url which is not part of the file.
        /// </summary>
        /// <param name="e">NavigationEventArgs</param>
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is FileActivatedEventArgs)
            {
                try
                {
                    FileActivatedEventArgs args = e.Parameter as FileActivatedEventArgs;
                    if (args.Files.Count > 0)
                    {
                        var xmlFile = args.Files[0] as StorageFile;
                        using (var stream = await xmlFile.OpenStreamForReadAsync())
                        {
                            XDocument document = XDocument.Load(stream);
                            // Where is the url to the feed source?
                        }
                    }
                } catch (Exception)
                {
                    MessageDialog msgbox = new MessageDialog("Reading xml file failed");
                    await msgbox.ShowAsync();
                }
            }
        }
    }
}
