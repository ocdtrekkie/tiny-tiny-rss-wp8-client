using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using TinyTinyRSS.Classes;
using TinyTinyRSS.Interface;
using TinyTinyRSS.Interface.Classes;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Diagnostics;
using Windows.UI.Core;
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
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if(SubscribeUrl.Text.Length == 0)
            {
                MessageDialog msgbox = new MessageDialog("Field url must not be empty!");
                await msgbox.ShowAsync();
            }
            try
            {
                int groupId = ((Category)SubscribeGroup.SelectedItem).id;
                string response = await TtRssInterface.getInterface().subscribeToFeed(SubscribeUrl.Text,
                    groupId, SubscribeUsername.Text, SubscribePassword.Text);
                if (response == null)
                {
                    await TtRssInterface.getInterface().getFeeds();
                }
                else
                {
                    MessageDialog msgbox = new MessageDialog("Subscribing failed. " + response);
                    await msgbox.ShowAsync();
                }
            } catch (TtRssException ex)
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
        }
    }
}
