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

namespace TinyTinyRSS
{
    public partial class SettingsPage : PhoneApplicationPage
    {
        ApplicationBarIconButton applyAppBarButton;
        ApplicationBarIconButton cancelAppBarButton;

        public SettingsPage()
        {
            InitializeComponent();
            BuildLocalizedApplicationBar();
            
            SetFields();
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
            PasswdField.Text = ConnectionSettings.getInstance().password;
            MarkReadCheckbox.IsChecked = ConnectionSettings.getInstance().markRead;
            ShowUnreadOnlyCheckbox.IsChecked = ConnectionSettings.getInstance().showUnreadOnly;
        }

        private async void AppBarButton_Click(object sender, EventArgs e)
        {
            if (sender.Equals(applyAppBarButton))
            {
                if (await TestSettings())
                {
                    ConnectionSettings.getInstance().username = UsernameField.Text;
                    ConnectionSettings.getInstance().server = ServerField.Text;
                    ConnectionSettings.getInstance().password = PasswdField.Text;
                    ConnectionSettings.getInstance().markRead = MarkReadCheckbox.IsChecked.Value;
                    ConnectionSettings.getInstance().showUnreadOnly = ShowUnreadOnlyCheckbox.IsChecked.Value;
                    NavigationService.GoBack(); // Reload Page!?
                }
                else
                {
                    // Popup falsche Settings
                }
            }
            else if (sender.Equals(cancelAppBarButton))
            {
                NavigationService.GoBack();
            }
        }

        private async Task<bool> TestSettings()
        {
            MyProgressbar.Visibility = Visibility.Visible;
            MyProgressbar.IsIndeterminate = true;           

            // Try to fix some common mistakes when entering an url.
            string server = ServerField.Text;
            if(!server.StartsWith("http://")) {
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
            string error = await TtRssInterface.getInterface().CheckLogin(server, UsernameField.Text, PasswdField.Text);
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
            Logger.WriteLine("NavigatedTo ArticlePage.");
        }
    }
}