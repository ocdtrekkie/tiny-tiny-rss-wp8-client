using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TinyTinyRSS.Classes;
using TinyTinyRSS.Interface;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Email;
using Windows.ApplicationModel.Resources;
using Windows.Foundation.Diagnostics;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Notifications;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace TinyTinyRSS
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        private ResourceLoader loader = new Windows.ApplicationModel.Resources.ResourceLoader();
        private LoggingChannel channel;
        public SettingsPage()
        {
            InitializeComponent();
            SetFields();
            string appVersion = string.Format("Version: {0}.{1}.{2}.{3}",
                    Package.Current.Id.Version.Major,
                    Package.Current.Id.Version.Minor,
                    Package.Current.Id.Version.Build,
                    Package.Current.Id.Version.Revision);
            this.AppVersion.Text = loader.GetString("SettingsAboutVersion") + appVersion;
            this.AppAuthor.Text = loader.GetString("SettingsAboutAuthor") + "Stefan Prasse";
            channel = new LoggingChannel("SettingsPage.cs", null);
            LogSession.addChannel(channel);
            this.Loaded += PageLoaded;
        }

        private void PageLoaded(object sender, RoutedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
                    rootFrame.CanGoBack ?
                        AppViewBackButtonVisibility.Visible :
                        AppViewBackButtonVisibility.Collapsed;
        }

        // init settings from saved values.
        private void SetFields()
        {
            UsernameField.Text = ConnectionSettings.getInstance().username;
            ServerField.Text = ConnectionSettings.getInstance().server;
            PasswdField.Password = ConnectionSettings.getInstance().password;
            MarkReadCheckbox.IsChecked = ConnectionSettings.getInstance().markRead;
            MarkReadScrollCheckbox.IsChecked = ConnectionSettings.getInstance().markReadByScrolling;
            ShowUnreadOnlyCheckbox.IsChecked = ConnectionSettings.getInstance().showUnreadOnly;
            ProgressAsCntrCheckbox.IsChecked = ConnectionSettings.getInstance().progressAsCntr;
            DarkArticleBackgroundCheckbox.IsChecked = ConnectionSettings.getInstance().useDarkBackground;
            SortBox.SelectedIndex = ConnectionSettings.getInstance().sortOrder;
            LiveTileCheckbox.IsChecked = ConnectionSettings.getInstance().liveTileActive;
            SwipeMarginSlider.Value = ConnectionSettings.getInstance().swipeMargin;
            UnsignedSslCb.IsChecked = ConnectionSettings.getInstance().allowSelfSignedCert;
            HttpAuthCb.IsChecked = ConnectionSettings.getInstance().httpAuth;
            FeatureRequestButton.IsEnabled = !ConnectionSettings.getInstance().featuresVoted;
        }
		
		// Test and save connection settings.        
        private async Task<bool> TestSettings()
        {
            MyProgressbar.Visibility = Visibility.Visible;
            ErrorMessage.Text = "";
            // Try to fix some common mistakes when entering an url.
            string server = ServerField.Text;
            if (!server.StartsWith("http://") && !server.StartsWith("https://"))
            {
                server = string.Concat("http://", server);
            }
            if (!server.EndsWith("/"))
            {
                server = string.Concat(server, "/");
            }
            if (!server.EndsWith("api/"))
            {
                server = string.Concat(server, "api/");
            }
            ServerField.Text = server;
            string error = await TtRssInterface.getInterface().CheckLogin(server, UsernameField.Text, PasswdField.Password);
            if (error.Length == 0)
            {
				ConnectionSettings.getInstance().username = UsernameField.Text;
				ConnectionSettings.getInstance().server = ServerField.Text;
				ConnectionSettings.getInstance().password = PasswdField.Password;
				Task<bool> tsk = PushNotificationHelper.AddNotificationChannel(ConnectionSettings.getInstance().username, ConnectionSettings.getInstance().password, ConnectionSettings.getInstance().server);
                TestButton.Content = loader.GetString("SuccessfulConnection");
                ErrorMessage.Text = "";
                if (!await tsk)
                {
                    MessageDialog msgbox = new MessageDialog(loader.GetString("SettingsUpdateLiveTileError"));
                    await msgbox.ShowAsync();
                }
                MyProgressbar.Visibility = Visibility.Collapsed;
                return true;
            }
            else
            {
                TestButton.Content = loader.GetString("FailedConnection");
                ErrorMessage.Text = error;
                MyProgressbar.Visibility = Visibility.Collapsed;
                return false;
            }
        }

		// Listener for changes on connection settings input fields.
        private void ConnectionSettingsChanged(object sender, TextChangedEventArgs e)
        {
            var buttonText = loader.GetString("TestConnectionSettingsButtonText");
            if (!TestButton.Content.Equals(buttonText))
            {
                if (buttonText.Length > 0)
                {
                    TestButton.Content = buttonText;
                }
            }
        }

		// Test and save connection settings.
        private async void TestButtonClicked(object sender, RoutedEventArgs e)
        {
            await TestSettings();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            channel.LogMessage("NavigatedTo Settings.", LoggingLevel.Information);
            base.OnNavigatedTo(e);
        }
		
		// Password changed listener
        private void PasswdField_PasswordChanged(object sender, SizeChangedEventArgs e)
        {
            if (!PasswdField.Password.Equals(ConnectionSettings.getInstance().password))
            {
                if (!TestButton.Content.Equals(loader.GetString("TestConnectionSettingsButton")))
                {
                    TestButton.Content = loader.GetString("TestConnectionSettingsButton");
                }
            }
        }

		// Send mail
        private async void AboutSendButton_Click(object sender, RoutedEventArgs e)
        {
            channel.LogMessage("Begin Send via email", LoggingLevel.Information);
            string Subject = "TT-RSS Universal App Feedback";
            bool sent = true;
            try
            {
                // Send an Email with attachment
                EmailMessage email = new EmailMessage();
                email.To.Add(new EmailRecipient("stefan@thescientist.eu"));
                email.Subject = Subject;
                StorageFile file = null;

                if (AboutRadio1.IsChecked.HasValue && AboutRadio1.IsChecked.Value)
                {
                    try
                    {
                        file = await LogSession.Save();
                    }
                    catch (Exception ex)
                    {
                        channel.LogMessage("Could not read logfile to send email." + ex.Message, LoggingLevel.Critical);
                    }
                }
                else if (AboutRadio2.IsChecked.HasValue && AboutRadio2.IsChecked.Value)
                {
                    StorageFolder storage = ApplicationData.Current.LocalFolder;
                    try
                    {
                        file = await storage.GetFileAsync(App.LastLogFile);
                    }
                    catch (Exception ex)
                    {
                        channel.LogMessage("Could not read logfile to send email." + ex.Message, LoggingLevel.Critical);
                    }
                }
                if(file!=null) {
                    email.Attachments.Add(new EmailAttachment(file.Name, file));   
                }
                email.Body = "Tell me anything :)";
                await EmailManager.ShowComposeNewEmailAsync(email);             
            }
            catch (Exception exc)
            {
                sent = false;
                channel.LogMessage("unable to create the email message." + exc.Message, LoggingLevel.Critical);
            }
            if (!sent)
            {
                MessageDialog msgbox = new MessageDialog(loader.GetString("SettingsMailException"));
                await msgbox.ShowAsync();                
            }
            channel.LogMessage("End Send via email.", LoggingLevel.Information);
        }

		// Live tile activated/deactivated
        private async void LiveTileCheckbox_Click(object sender, RoutedEventArgs e)
        {
            LiveTileProgress.Visibility = Visibility.Visible;
            string deviceId = PushNotificationHelper.GetDeviceID();
            if (LiveTileCheckbox.IsChecked.HasValue && LiveTileCheckbox.IsChecked.Value)
            {
                if (await TestSettings())
                {
                    ConnectionSettings.getInstance().liveTileActive = true;
                    if (await PushNotificationHelper.AddNotificationChannel(UsernameField.Text, PasswdField.Password, ServerField.Text))
                    {
                        await PushNotificationHelper.UpdateLiveTile(-1);
                    }
                    else
                    {
                        ConnectionSettings.getInstance().liveTileActive = false;
                        LiveTileCheckbox.IsChecked = false;
                        MessageDialog msgbox = new MessageDialog(loader.GetString("ErrorAddLiveTile"));
                        await msgbox.ShowAsync();     
                    }
                }
                else
                {
                    MessageDialog msgbox = new MessageDialog(loader.GetString("NoConnection"));
                    await msgbox.ShowAsync();     
                    LiveTileCheckbox.IsChecked = false;
                }
            }
            else
            {
                if (ConnectionSettings.getInstance().liveTileActive == true)
                {
                    TileUpdateManager.CreateTileUpdaterForApplication().Clear();
                    BadgeUpdateManager.CreateBadgeUpdaterForApplication().Clear();
                    ConnectionSettings.getInstance().liveTileActive = false;
                    Task t = PushNotificationHelper.ClosePushNotifications();
                    var httpFormUrlEncodedContent = new HttpFormUrlEncodedContent(new[] {
                        new KeyValuePair<string, string>("action", "deleteUser"),
                        new KeyValuePair<string, string>("deviceId", deviceId),
                        new KeyValuePair<string, string>("hash", PushNotificationHelper.HASH)
                    });
                    try
                    {
                        HttpClient httpClient = new HttpClient();
                        HttpRequestMessage msg = new HttpRequestMessage(new HttpMethod("POST"), new Uri(PushNotificationHelper.SERVERURL));
                        msg.Content = httpFormUrlEncodedContent;
                        HttpResponseMessage httpresponse = await httpClient.SendRequestAsync(msg).AsTask();
                        var responseString = await httpresponse.Content.ReadAsStringAsync();
                        
                        if (!responseString.Equals("1"))
                        {
                            channel.LogMessage("error deleting livetile user");
                            channel.LogMessage(responseString);
                        }
                    }
                    catch (Exception ex)
                    {
                        channel.LogMessage("error deleting livetile user");
                        channel.LogMessage(ex.Message);
                    }
                    await t;
                }
            }
            LiveTileProgress.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Button listener that opens the webpage of this project.
        /// </summary>
        /// <param name="sender">default method argument</param>
        /// <param name="e">default method argument</param>
        private async void ProjectPageButton_Click(object sender, RoutedEventArgs e)
        {
            var uri = new Uri("https://thescientist.eu/tt-rss-reader-for-wp-8/");
            await Windows.System.Launcher.LaunchUriAsync(uri);
        }

		// settings changed
        private void Changed(object sender, RoutedEventArgs e)
        {
			if(sender==MarkReadCheckbox) {
				ConnectionSettings.getInstance().markRead = MarkReadCheckbox.IsChecked.Value;
			} else if(sender==ShowUnreadOnlyCheckbox) {
				ConnectionSettings.getInstance().showUnreadOnly = ShowUnreadOnlyCheckbox.IsChecked.Value;
			} else if(sender==ProgressAsCntrCheckbox) {
				ConnectionSettings.getInstance().progressAsCntr = ProgressAsCntrCheckbox.IsChecked.Value;
            } else if (sender == DarkArticleBackgroundCheckbox) {
                ConnectionSettings.getInstance().useDarkBackground = DarkArticleBackgroundCheckbox.IsChecked.Value;
            } else if (sender == UnsignedSslCb) {
                ConnectionSettings.getInstance().allowSelfSignedCert = UnsignedSslCb.IsChecked.Value;
            } else if (sender == HttpAuthCb) {
                ConnectionSettings.getInstance().httpAuth = HttpAuthCb.IsChecked.Value;
            } else if (sender == MarkReadScrollCheckbox) {
                ConnectionSettings.getInstance().markReadByScrolling = MarkReadScrollCheckbox.IsChecked.Value;
            }
        }

		// Sort options changed
        private void SelChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SortBox != null && sender== SortBox) { 
                ConnectionSettings.getInstance().sortOrder = SortBox.SelectedIndex;
            }  
        }

        private async void btnGoToLockSettings_Click(object sender, RoutedEventArgs e)
        {
            // Launch URI for the lock screen settings screen.
            await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings-lock:"));
        }

        private void Slider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            ConnectionSettings.getInstance().swipeMargin = Convert.ToInt32(e.NewValue);
        }

        private void FeatureRequest_Click(object sender, RoutedEventArgs e)
        {
            if (LiveTileFeatureCheckbox.IsChecked.HasValue && LiveTileFeatureCheckbox.IsChecked.Value)
            {
                Microsoft.HockeyApp.HockeyClient.Current.TrackEvent("Feature_AdvancedLiveTiles");
            }
            else if (FeedTreeFeatureCheckbox.IsChecked.HasValue && FeedTreeFeatureCheckbox.IsChecked.Value)
            {
                Microsoft.HockeyApp.HockeyClient.Current.TrackEvent("Feature_FeedTree");
            }
            else if (LabelNotesFeatureCheckbox.IsChecked.HasValue && LabelNotesFeatureCheckbox.IsChecked.Value)
            {
                Microsoft.HockeyApp.HockeyClient.Current.TrackEvent("Feature_LabelsAndNotes");
            }
            else if (ShortcutsFeatureCheckbox.IsChecked.HasValue && ShortcutsFeatureCheckbox.IsChecked.Value)
            {
                Microsoft.HockeyApp.HockeyClient.Current.TrackEvent("Feature_KeyboardShortcuts");
            }
            ConnectionSettings.getInstance().featuresVoted = true;
            FeatureRequestButton.IsEnabled = !ConnectionSettings.getInstance().featuresVoted;
        }

        private void FeatureCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox[] options = new CheckBox[] { LiveTileFeatureCheckbox, FeedTreeFeatureCheckbox,
                LabelNotesFeatureCheckbox, ShortcutsFeatureCheckbox };
            int selected = 0;
            foreach (CheckBox box in options)
            {
                if(box.IsChecked.HasValue && box.IsChecked.Value)
                {
                    selected++;
                }
            }
            if(selected>2)
            {
                ((CheckBox)sender).IsChecked = false;
            }
        }
    }
}
