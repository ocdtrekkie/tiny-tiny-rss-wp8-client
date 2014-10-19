using CaledosLab.Portable.Logging;
using NotificationsExtensions.BadgeContent;
using NotificationsExtensions.TileContent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using TinyTinyRSS.Interface;
using TinyTinyRSS.Resources;
using TinyTinyRSSInterface;
using Windows.Networking.PushNotifications;
using Windows.Phone.System.Analytics;
using Windows.UI.Notifications;

namespace TinyTinyRSS.Classes
{
    public class PushNotificationHelper
    {
        //public const String SERVERURL = "http://localhost/ttrss/api.php";
        public const String SERVERURL = "https://thescientist.eu/ttrss-api/api.php";
        public const String HASH = "2u409g0hbinyv";
        public static async Task<bool> UpdateNotificationChannel()
        {
            if (!ConnectionSettings.getInstance().liveTileActive)
            {
                return true;
            }
            string deviceId = HostInformation.PublisherHostId;

            PushNotificationChannel channel = null;
            try
            {
                channel = await PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();
                if (channel.Uri == ConnectionSettings.getInstance().channelUri)
                {
                    return true;
                }
                var values = new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("action", "updateChannel"),
                        new KeyValuePair<string, string>("deviceId", deviceId),
                        new KeyValuePair<string, string>("channel", channel.Uri),
                        new KeyValuePair<string, string>("hash", HASH)
                    };

                var httpClient = new HttpClient(new System.Net.Http.HttpClientHandler());
                HttpResponseMessage response = await httpClient.PostAsync(SERVERURL, new FormUrlEncodedContent(values));
                response.EnsureSuccessStatusCode();
                var responseString = await response.Content.ReadAsStringAsync();
                if (!responseString.Equals("1"))
                {
                    Logger.WriteLine("Could not update notific. channel" + responseString);
                    return false;
                }
                else
                {
                    ConnectionSettings.getInstance().channelUri = channel.Uri;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLine("Could not update notific. channel" + ex.Message);
                return false;
            }
            return true;
        }

        public static async Task<bool> AddNotificationChannel(string user, string password, string server)
        {
            string deviceId = HostInformation.PublisherHostId;

            PushNotificationChannel channel = null;
            try
            {
                channel = await PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();
                var values = new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("action", "updateUser"),
                        new KeyValuePair<string, string>("deviceId", deviceId),
                        new KeyValuePair<string, string>("loginName", user),
                        new KeyValuePair<string, string>("loginPassword", password),
                        new KeyValuePair<string, string>("server", server),
                        new KeyValuePair<string, string>("channel", channel.Uri),
                        new KeyValuePair<string, string>("hash", HASH)
                    };
                try
                {
                    var httpClient = new HttpClient(new System.Net.Http.HttpClientHandler());
                    HttpResponseMessage response = await httpClient.PostAsync(SERVERURL, new FormUrlEncodedContent(values));
                    response.EnsureSuccessStatusCode();
                    var responseString = await response.Content.ReadAsStringAsync();
                    if (!responseString.Equals("1"))
                    {
                        MessageBox.Show(AppResources.ErrorAddLiveTile);
                        Logger.WriteLine(responseString);
                        return false;
                    }
                    else
                    {
                        ConnectionSettings.getInstance().channelUri = channel.Uri;
                    }
                }
                catch (HttpRequestException ex)
                {
                    MessageBox.Show(AppResources.ErrorAddLiveTile);
                    Logger.WriteLine(ex.Message);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLine(ex.Message);
                return false;
            }
            return true;
        }

        public static async Task ClosePushNotifications()
        {
            try {
                PushNotificationChannel channel = await PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();
                channel.Close();
                ConnectionSettings.getInstance().channelUri = "";
                TileUpdateManager.CreateTileUpdaterForApplication().Clear();
                BadgeUpdateManager.CreateBadgeUpdaterForApplication().Clear();
            }
            catch (Exception exc)
            {
                Logger.WriteLine("Could not close pushnotificationchannel.");
                Logger.WriteLine(exc.Message);
            }
        }

        public static async Task UpdateLiveTile(int fresh)
        {
            if (!ConnectionSettings.getInstance().liveTileActive)
            {
                return;
            }
            if (fresh == -1)
            {
                fresh = await TtRssInterface.getInterface().getUnReadCount(true);
            }
            ITileSquare150x150IconWithBadge tileContent = TileContentFactory.CreateTileSquare150x150IconWithBadge();

            tileContent.ImageIcon.Src = "ms-appx:///Assets/LiveTile.png";
            TileUpdateManager.CreateTileUpdaterForApplication().Update(tileContent.CreateNotification());

            BadgeNumericNotificationContent badgeContent = new BadgeNumericNotificationContent((uint)fresh);
            BadgeUpdateManager.CreateBadgeUpdaterForApplication().Update(badgeContent.CreateNotification());

        }
    }
}
