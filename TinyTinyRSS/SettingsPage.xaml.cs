using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.IO.IsolatedStorage;
using TinyTinyRSS.Resources;
using System.Security.Cryptography;
using System.Text;
using TinyTinyRSS.Interface;
using TinyTinyRSS.Interface.Classes;
using System.Threading.Tasks;
using TinyTinyRSSInterface;
using CaledosLab.Portable.Logging;
using Microsoft.Phone.Tasks;
using System.IO;
using System.Reflection;
using Windows.UI.Notifications;
using System.Net.Http;
using Microsoft.Phone.Info;
using Windows.Phone.System.Analytics;
using Windows.Data.Xml.Dom;
using NotificationsExtensions.BadgeContent;
using System.ComponentModel;
using TinyTinyRSS.Classes;
using Windows.Networking.PushNotifications;

namespace TinyTinyRSS
{
    public partial class SettingsPage : PhoneApplicationPage
    {       
        public SettingsPage()
        {
            InitializeComponent();
            SetFields();
            this.AppVersion.Text = AppResources.SettingsAboutVersion + Assembly.GetExecutingAssembly().GetName().Version.ToString();
            this.AppAuthor.Text = AppResources.SettingsAboutAuthor + "Stefan Prasse"; 
            if (SystemTray.GetProgressIndicator(this) == null)
            {
                SystemTray.SetProgressIndicator(this, new ProgressIndicator());
            }
        }

		// init settings from saved values.
        private void SetFields()
        {
            UsernameField.Text = ConnectionSettings.getInstance().username;
            ServerField.Text = ConnectionSettings.getInstance().server;
            PasswdField.Password = ConnectionSettings.getInstance().password;
            MarkReadCheckbox.IsChecked = ConnectionSettings.getInstance().markRead;
            ShowUnreadOnlyCheckbox.IsChecked = ConnectionSettings.getInstance().showUnreadOnly;
            ProgressAsCntrCheckbox.IsChecked = ConnectionSettings.getInstance().progressAsCntr;
            SortBox.SelectedIndex = ConnectionSettings.getInstance().sortOrder;
            LiveTileCheckbox.IsChecked = ConnectionSettings.getInstance().liveTileActive;
        }
		
		// Test and save connection settings.        
        private async Task<bool> TestSettings()
        {
            MyProgressbar.Visibility = Visibility.Visible;
            MyProgressbar.IsIndeterminate = true;

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
				Task tsk = PushNotificationHelper.AddNotificationChannel(ConnectionSettings.getInstance().username, ConnectionSettings.getInstance().password, ConnectionSettings.getInstance().server);
                TestButton.Content = AppResources.SuccessfulConnection;
                ErrorMessage.Text = "";
				await tsk;
				// TODO MessageBox bei Fehler.
                MyProgressbar.Visibility = Visibility.Collapsed;
                MyProgressbar.IsIndeterminate = false;				
                return true;
            }
            else
            {
                TestButton.Content = AppResources.FailedConnection;
                ErrorMessage.Text = error;
                MyProgressbar.Visibility = Visibility.Collapsed;
                MyProgressbar.IsIndeterminate = false;
                return false;
            }
        }

		// Listener for changes on connection settings input fields.
        private void ConnectionSettingsChanged(object sender, TextChangedEventArgs e)
        {
            if (!TestButton.Content.Equals(AppResources.TestConnectionSettingsButton))
            {
                TestButton.Content = AppResources.TestConnectionSettingsButton;
            }
        }

		// Test and save connection settings.
        private async void TestButtonClicked(object sender, RoutedEventArgs e)
        {
            await TestSettings();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            Logger.WriteLine("NavigatedTo Settings.");
        }
		
		// Password changed listener
        private void PasswdField_PasswordChanged(object sender, SizeChangedEventArgs e)
        {
            if (!PasswdField.Password.Equals(ConnectionSettings.getInstance().password))
            {
                if (!TestButton.Content.Equals(AppResources.TestConnectionSettingsButton))
                {
                    TestButton.Content = AppResources.TestConnectionSettingsButton;
                }
            }
        }

		// Send mail
        private void AboutSendButton_Click(object sender, RoutedEventArgs e)
        {
            Logger.WriteLine("Begin Send via email");
            string Subject = "TT-RSS WP8 App Feedback";
            try
            {
                EmailComposeTask mail = new EmailComposeTask();
                mail.Subject = Subject;
                mail.To = "stefan@thescientist.eu";

                if (AboutRadio1.IsChecked.HasValue && AboutRadio1.IsChecked.Value)
                {
                    mail.Body = Logger.GetStoredLog();
                }
                else if (AboutRadio2.IsChecked.HasValue && AboutRadio2.IsChecked.Value)
                {
                    using (IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        if (storage.FileExists(App.ErrorLogFile))
                        {
                            using (IsolatedStorageFileStream fs = storage.OpenFile(App.ErrorLogFile, FileMode.Open))
                            {
                                using (StreamReader reader = new StreamReader(fs))
                                {
                                    mail.Body = reader.ReadToEnd();
                                }
                            }
                        }
                    }
                }
                if (mail.Body == null)
                {
                    mail.Body = "";
                }
                if (mail.Body.Length > 16000) // max 64K 
                {
                    mail.Body = mail.Body.Substring(mail.Body.Length - 16000);
                }

                mail.Show();
            }
            catch
            {
                MessageBox.Show(AppResources.SettingsMailException);
                Logger.WriteLine("unable to create the email message");
            }

            Logger.WriteLine("End Send via email");
        }

		// Live tile activated/deactivated
        private async void LiveTileCheckbox_Click(object sender, RoutedEventArgs e)
        {
            SystemTray.ProgressIndicator.IsVisible = true;
            SystemTray.ProgressIndicator.IsIndeterminate = true; 
            string deviceId = HostInformation.PublisherHostId;
            if (LiveTileCheckbox.IsChecked.HasValue && LiveTileCheckbox.IsChecked.Value)
            {
                if (await TestSettings())
                {
                    if (await PushNotificationHelper.AddNotificationChannel(UsernameField.Text, PasswdField.Password, ServerField.Text))
                    {
                        ConnectionSettings.getInstance().liveTileActive = true;
                        await PushNotificationHelper.UpdateLiveTile(-1);
                    }
                    else
                    {
                        LiveTileCheckbox.IsChecked = false;
                        MessageBox.Show(AppResources.ErrorAddLiveTile);
                    }
                }
                else
                {
                    MessageBox.Show(AppResources.NoConnection);
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
                    var values = new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("action", "deleteUser"),
                        new KeyValuePair<string, string>("deviceId", deviceId),
                        new KeyValuePair<string, string>("hash", PushNotificationHelper.HASH)
                    };
                    try
                    {
                        var httpClient = new HttpClient(new System.Net.Http.HttpClientHandler());
                        HttpResponseMessage response = await httpClient.PostAsync(PushNotificationHelper.SERVERURL, new FormUrlEncodedContent(values));
                        response.EnsureSuccessStatusCode();
                        var responseString = await response.Content.ReadAsStringAsync();
                        if (!responseString.Equals("1"))
                        {
                            Logger.WriteLine("error deleting livetile user");
                            Logger.WriteLine(responseString);
                        }
                    }
                    catch (HttpRequestException ex)
                    {
                        Logger.WriteLine("error deleting livetile user");
                        Logger.WriteLine(ex.Message);
                    }
                    await t;
                }
            }
            SystemTray.ProgressIndicator.IsVisible = false;
            SystemTray.ProgressIndicator.IsIndeterminate = false;
        }

        /// <summary>
        /// Button listener that opens the webpage of this project.
        /// </summary>
        /// <param name="sender">default method argument</param>
        /// <param name="e">default method argument</param>
        private void ProjectPageButton_Click(object sender, RoutedEventArgs e)
        {
            WebBrowserTask wbt = new WebBrowserTask();
            wbt.Uri = new Uri("https://thescientist.eu/tt-rss-reader-for-wp-8/");
            wbt.Show();
        }

		// settings changed
        private async void Changed(object sender, RoutedEventArgs e)
        {
			if(sender==MarkReadCheckbox) {
				ConnectionSettings.getInstance().markRead = MarkReadCheckbox.IsChecked.Value;
			} else if(sender==ShowUnreadOnlyCheckbox) {
				ConnectionSettings.getInstance().showUnreadOnly = ShowUnreadOnlyCheckbox.IsChecked.Value;
			} else if(sender==ProgressAsCntrCheckbox) {
				ConnectionSettings.getInstance().progressAsCntr = ProgressAsCntrCheckbox.IsChecked.Value;
			}  
        }

		// Sort options changed
        private async void SelChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SortBox != null) { 
            ConnectionSettings.getInstance().sortOrder = SortBox.SelectedIndex;
        }
        }

        private async void btnGoToLockSettings_Click(object sender, RoutedEventArgs e)
        {
            // Launch URI for the lock screen settings screen.
            await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings-lock:"));
        }
    }
}