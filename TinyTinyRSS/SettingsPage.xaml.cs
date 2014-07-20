using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
using System.ComponentModel;

namespace TinyTinyRSS
{
    public partial class SettingsPage : PhoneApplicationPage
    {
        ApplicationBarIconButton applyAppBarButton;
        ApplicationBarIconButton cancelAppBarButton;
        bool unsavedSettings = false;

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
            SortBox.SelectedIndex = ConnectionSettings.getInstance().sortOrder;
            ServerField.Text = ConnectionSettings.getInstance().server;
            PasswdField.Password = ConnectionSettings.getInstance().password;
            MarkReadCheckbox.IsChecked = ConnectionSettings.getInstance().markRead;
            ShowUnreadOnlyCheckbox.IsChecked = ConnectionSettings.getInstance().showUnreadOnly;
            ProgressAsCntrCheckbox.IsChecked = ConnectionSettings.getInstance().progressAsCntr;
            unsavedSettings = false;
        }

        private void AppBarButton_Click(object sender, EventArgs e)
        {
            if (sender.Equals(applyAppBarButton))
            {
                SaveAllSettings();
            }
            else if (sender.Equals(cancelAppBarButton))
            {
                if (NavigationService.CanGoBack)
                {
                    NavigationService.GoBack();
                }
                else
                {
                    NavigationService.Navigate(new Uri("/MainPage.xml"));
                }
            }
        }

        private void SaveAllSettings()
        {
            ConnectionSettings.getInstance().username = UsernameField.Text;
                ConnectionSettings.getInstance().server = ServerField.Text;
                ConnectionSettings.getInstance().password = PasswdField.Password;
                ConnectionSettings.getInstance().markRead = MarkReadCheckbox.IsChecked.Value;
                ConnectionSettings.getInstance().sortOrder = SortBox.SelectedIndex;
                ConnectionSettings.getInstance().showUnreadOnly = ShowUnreadOnlyCheckbox.IsChecked.Value;
                ConnectionSettings.getInstance().progressAsCntr = ProgressAsCntrCheckbox.IsChecked.Value;
                if (NavigationService.CanGoBack)
                {
                    NavigationService.GoBack();
                }
                else
                {
                    NavigationService.Navigate(new Uri("/MainPage.xml"));
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
            unsavedSettings = true;
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
            unsavedSettings = true;
        }

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
                if(mail.Body == null)
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

        protected override void OnBackKeyPress(CancelEventArgs e)
        {
            if (unsavedSettings)
            {
                if (MessageBox.Show(AppResources.UnsavedSettings, AppResources.SaveUnsavedSettings,
                                    MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                {
                    SaveAllSettings();
                }
            }            
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

        private void Changed(object sender, RoutedEventArgs e)
        {
            unsavedSettings = true;
        }

        private void SelChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count != 0 || e.RemovedItems.Count != 0)
            {
                unsavedSettings = true;
            }
        }
    }
}