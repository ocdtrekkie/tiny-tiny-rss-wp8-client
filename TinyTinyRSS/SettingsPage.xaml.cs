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

namespace TinyTinyRSS
{
    public partial class SettingsPage : PhoneApplicationPage
    {
        ApplicationBarIconButton applyAppBarButton;
        ApplicationBarIconButton cancelAppBarButton;
        private const String serverUrl = "https://thescientist.eu/ttrss-api/api.php";
        public SettingsPage()
        {
            InitializeComponent();
            BuildLocalizedApplicationBar();
            SetFields();
            this.AppVersion.Text = AppResources.SettingsAboutVersion + Assembly.GetExecutingAssembly().GetName().Version.ToString();
            this.AppAuthor.Text = AppResources.SettingsAboutAuthor + "Stefan Prasse";
        }

        private void BuildLocalizedApplicationBar()
        {
            // ApplicationBar der Seite einer neuen Instanz von ApplicationBar zuweisen
            ApplicationBar = new ApplicationBar();

            // Eine neue Schaltfläche erstellen und als Text die lokalisierte Zeichenfolge aus AppResources zuweisen.
            applyAppBarButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/check.png", UriKind.Relative));
            applyAppBarButton.Text = AppResources.SaveAppBarButtonText;
            applyAppBarButton.Click += AppBarButton_Click;
            ApplicationBar.Buttons.Add(applyAppBarButton);

            cancelAppBarButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/cancel.png", UriKind.Relative));
            cancelAppBarButton.Text = AppResources.CancelAppBarButtonText;
            cancelAppBarButton.Click += AppBarButton_Click;
            ApplicationBar.Buttons.Add(cancelAppBarButton);
        }

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
            switch (ConnectionSettings.getInstance().tileUpdateInterval)
            {
                case 1:
                    Hour.IsChecked = true;
                    break;
                case 2:
                    Dayly.IsChecked = true;
                    break;
                default:
                    HalfHour.IsChecked = true;
                    break;
            }
        }

        private async void AppBarButton_Click(object sender, EventArgs e)
        {
            if (sender.Equals(applyAppBarButton))
            {
                if (await TestSettings())
                {
                    ConnectionSettings.getInstance().username = UsernameField.Text;
                    ConnectionSettings.getInstance().server = ServerField.Text;
                    ConnectionSettings.getInstance().password = PasswdField.Password;
                    ConnectionSettings.getInstance().markRead = MarkReadCheckbox.IsChecked.Value;
                    ConnectionSettings.getInstance().sortOrder = SortBox.SelectedIndex;
                    ConnectionSettings.getInstance().showUnreadOnly = ShowUnreadOnlyCheckbox.IsChecked.Value;
                    ConnectionSettings.getInstance().progressAsCntr = ProgressAsCntrCheckbox.IsChecked.Value;
                    if (NavigationService.CanGoBack) { 
                    NavigationService.GoBack(); // Reload Page!?
                    }
                    else
                    {
                        NavigationService.Navigate(new Uri("/MainPage.xaml"));
                    }
                }
                else
                {
                    // Popup falsche Settings
                }
            }
            else if (sender.Equals(cancelAppBarButton))
            {
                if (NavigationService.CanGoBack) { 
                    NavigationService.GoBack(); // Reload Page!?
                    }
                    else
                    {
                        NavigationService.Navigate(new Uri("/MainPage.xaml"));
                    }
            }
        }

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
                TestButton.Content = AppResources.SuccessfulConnection;
                ErrorMessage.Text = "";
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

        private void ConnectionSettingsChanged(object sender, TextChangedEventArgs e)
        {
            if (!TestButton.Content.Equals(AppResources.TestConnectionSettingsButton))
            {
                TestButton.Content = AppResources.TestConnectionSettingsButton;
            }
        }

        private async void TestButtonClicked(object sender, RoutedEventArgs e)
        {
            await TestSettings();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            Logger.WriteLine("NavigatedTo Settings.");
        }

        private void PasswdField_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!TestButton.Content.Equals(AppResources.TestConnectionSettingsButton))
            {
                TestButton.Content = AppResources.TestConnectionSettingsButton;
            }
        }

        private void AboutSendButton_Click(object sender, RoutedEventArgs e)
        {
            //http://www.geekchamp.com/marketplace/components/livemailmessage
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

        private async void LiveTileCheckbox_Click(object sender, RoutedEventArgs e)
        {
            string deviceId = HostInformation.PublisherHostId;
            if (LiveTileCheckbox.IsChecked.HasValue && LiveTileCheckbox.IsChecked.Value)
            {
                if (await TestSettings())
                {
                    var values = new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("action", "updateUser"),
                        new KeyValuePair<string, string>("deviceId", deviceId),
                        new KeyValuePair<string, string>("loginName", UsernameField.Text),
                        new KeyValuePair<string, string>("loginPassword", PasswdField.Password),
                        new KeyValuePair<string, string>("server", ServerField.Text)
                    };
                    try
                    {
                        var httpClient = new HttpClient(new System.Net.Http.HttpClientHandler());
                        HttpResponseMessage response = await httpClient.PostAsync(serverUrl, new FormUrlEncodedContent(values));
                        response.EnsureSuccessStatusCode();
                        var responseString = await response.Content.ReadAsStringAsync();
                        if (!responseString.Equals("1"))
                        {
                            MessageBox.Show(AppResources.ErrorAddLiveTile);
                            Logger.WriteLine(responseString);
                            LiveTileCheckbox.IsChecked = false;
                        }
                        else
                        {
                            ConnectionSettings.getInstance().liveTileActive = true;
                            PeriodicUpdateRecurrence recurrence;
                            if (Dayly.IsChecked.HasValue && Dayly.IsChecked.Value)
                            {
                                recurrence = PeriodicUpdateRecurrence.Daily;
                            }
                            else if (HalfHour.IsChecked.HasValue && HalfHour.IsChecked.Value)
                            {
                                recurrence = PeriodicUpdateRecurrence.HalfHour;
                            }
                            else
                            {
                                recurrence = PeriodicUpdateRecurrence.Hour;
                            }
                            //System.Uri url = new System.Uri(serverUrl + "?action=getUnreadCount&deviceId=" + deviceId);
                            System.Uri url = new System.Uri("https://thescientist.eu/ttrss-api/tile.xml");
                            TileUpdateManager.CreateTileUpdaterForApplication().StartPeriodicUpdate(url, recurrence);
                        }
                    }
                    catch (HttpRequestException ex)
                    {
                        MessageBox.Show(AppResources.ErrorAddLiveTile);
                        Logger.WriteLine(ex.Message);
                        LiveTileCheckbox.IsChecked = false;
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
                    try
                    {
                        var httpClient = new HttpClient(new System.Net.Http.HttpClientHandler());
                        HttpResponseMessage response = await httpClient.GetAsync(serverUrl + "?action=deleteUser&deviceId=" + Uri.EscapeDataString(deviceId));
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
                    TileUpdateManager.CreateTileUpdaterForApplication().StopPeriodicUpdate();
                    ConnectionSettings.getInstance().liveTileActive = false;
                }
            }
        }

        private void UpdateInterval_Click(object sender, RoutedEventArgs e)
        {
            string deviceId = DeviceExtendedProperties.GetValue("DeviceUniqueId").ToString();
            PeriodicUpdateRecurrence recurrence;
            if (sender == Dayly)
            {
                ConnectionSettings.getInstance().tileUpdateInterval = 2;
                recurrence = PeriodicUpdateRecurrence.Daily;
            }
            else if (sender == HalfHour)
            {
                ConnectionSettings.getInstance().tileUpdateInterval = 0;
                recurrence = PeriodicUpdateRecurrence.HalfHour;
            }
            else
            {
                ConnectionSettings.getInstance().tileUpdateInterval = 1;
                recurrence = PeriodicUpdateRecurrence.Hour;
            }
            // Update UpdateReccurence
            TileUpdateManager.CreateTileUpdaterForApplication().StopPeriodicUpdate();
            //System.Uri url = new System.Uri(serverUrl + "?action=getUnreadCount&deviceId=" + deviceId);
            System.Uri url = new System.Uri("https://thescientist.eu/ttrss-api/tile.xml");
            TileUpdateManager.CreateTileUpdaterForApplication().StartPeriodicUpdate(url, recurrence);
        }
    }
}