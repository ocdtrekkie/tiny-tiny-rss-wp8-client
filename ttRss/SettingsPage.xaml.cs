using CaledosLab.Portable.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using TinyTinyRSS.Classes;
using TinyTinyRSS.Interface;
using TinyTinyRSS;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Email;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Storage;
using Windows.UI.Notifications;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;
using Windows.Phone.UI.Input;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace TinyTinyRSS
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        private ResourceLoader loader = new Windows.ApplicationModel.Resources.ResourceLoader();
        private StatusBar statusBar;
        public SettingsPage()
        {
            InitializeComponent();
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;
            statusBar = Windows.UI.ViewManagement.StatusBar.GetForCurrentView();
            SetFields();
            string appVersion = string.Format("Version: {0}.{1}.{2}.{3}",
                    Package.Current.Id.Version.Major,
                    Package.Current.Id.Version.Minor,
                    Package.Current.Id.Version.Build,
                    Package.Current.Id.Version.Revision);
            this.AppVersion.Text = loader.GetString("SettingsAboutVersion") + appVersion;
            this.AppAuthor.Text = loader.GetString("SettingsAboutAuthor") + "Stefan Prasse"; 
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
            DarkArticleBackgroundCheckbox.IsChecked = ConnectionSettings.getInstance().useDarkBackground;
            SortBox.SelectedIndex = ConnectionSettings.getInstance().sortOrder;
            SettingHeadlinesViewBox.SelectedIndex = ConnectionSettings.getInstance().headlinesView;
            LiveTileCheckbox.IsChecked = ConnectionSettings.getInstance().liveTileActive;
            SwipeMarginSlider.Value = ConnectionSettings.getInstance().swipeMargin;
        }
		
		// Test and save connection settings.        
        private async Task<bool> TestSettings()
        {
            await statusBar.ProgressIndicator.ShowAsync();
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
                await statusBar.ProgressIndicator.HideAsync();	
                return true;
            }
            else
            {
                TestButton.Content = loader.GetString("FailedConnection");
                ErrorMessage.Text = error;
                await statusBar.ProgressIndicator.HideAsync();	
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
            Logger.WriteLine("NavigatedTo Settings.");
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
            Logger.WriteLine("Begin Send via email");
            string Subject = "TT-RSS WP8 App Feedback";
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
                    App.FinalizeLogging();
                    StorageFolder storage = ApplicationData.Current.LocalFolder;
                    try
                    {
                        file = await storage.GetFileAsync(App.LogFile);
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLine("Could not read logfile to send email.", ex);
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
                        Logger.WriteLine("Could not read lastlogfile to send email.", ex);
                    }
                }
                if(file!=null) {
                email.Attachments.Add(new EmailAttachment(file.Name, file));   
                }
                email.Body = "Tell me anything :)";
                await EmailManager.ShowComposeNewEmailAsync(email);             
            }
            catch
            {
                sent = false;
                Logger.WriteLine("unable to create the email message");
            }
            if (!sent)
            {
                MessageDialog msgbox = new MessageDialog(loader.GetString("SettingsMailException"));
                await msgbox.ShowAsync();                
            }
            Logger.WriteLine("End Send via email");
        }

		// Live tile activated/deactivated
        private async void LiveTileCheckbox_Click(object sender, RoutedEventArgs e)
        {
            await statusBar.ProgressIndicator.ShowAsync();
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
                            Logger.WriteLine("error deleting livetile user");
                            Logger.WriteLine(responseString);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLine("error deleting livetile user");
                        Logger.WriteLine(ex.Message);
                    }
                    await t;
                }
            }
            await statusBar.ProgressIndicator.HideAsync();
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
            }  
        }

		// Sort options changed
        private void SelChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SortBox != null && sender== SortBox) { 
                ConnectionSettings.getInstance().sortOrder = SortBox.SelectedIndex;
            } else if (SettingHeadlinesViewBox != null && sender== SettingHeadlinesViewBox) { 
                ConnectionSettings.getInstance().headlinesView = SettingHeadlinesViewBox.SelectedIndex;
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
    }
}
