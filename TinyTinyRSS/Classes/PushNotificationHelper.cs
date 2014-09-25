using CaledosLab.Portable.Logging;
using NotificationsExtensions.BadgeContent;
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
        public const String SERVERURL = "https://thescientist.eu/ttrss-api/api.php";

        public static async Task<bool> UpdateNotificationChannel()
        {
            if (!ConnectionSettings.getInstance().liveTileActive)
            {
                return true;
            }
            string deviceId = HostInformation.PublisherHostId;
            string deviceIdEscaped = Uri.EscapeDataString(deviceId);

            PushNotificationChannel channel = null;
            try
            {
                channel = await PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();
                if (channel.Uri == ConnectionSettings.getInstance().channelUri)
                {
                    return true;
                }
                string channelEscaped = Uri.EscapeDataString(channel.Uri);
                var values = new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("action", "updateChannel"),
                        new KeyValuePair<string, string>("deviceId", deviceId),
                        new KeyValuePair<string, string>("channel", channelEscaped)
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
            string deviceIdEscaped = Uri.EscapeDataString(deviceId);

            PushNotificationChannel channel = null;
            try
            {
                channel = await PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();
                string channelEscaped = Uri.EscapeDataString(channel.Uri);
                var values = new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("action", "updateUser"),
                        new KeyValuePair<string, string>("deviceId", deviceId),
                        new KeyValuePair<string, string>("loginName", user),
                        new KeyValuePair<string, string>("loginPassword", password),
                        new KeyValuePair<string, string>("server", server),
                        new KeyValuePair<string, string>("channel", channelEscaped)
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

        public static async Task UpdateLiveTile()
        {
            try
            {
                int unread = await TtRssInterface.getInterface().getUnReadCount(true);
                BadgeNumericNotificationContent badgeContent = new BadgeNumericNotificationContent(Convert.ToUInt32(unread));
                BadgeUpdateManager.CreateBadgeUpdaterForApplication().Update(badgeContent.CreateNotification());
            }
            catch (Exception exc)
            {
                Logger.WriteLine("Could not get actual unreadCount while activating live tile.");
                Logger.WriteLine(exc.Message);
            }
        }

        public static async Task ClosePushNotifications()
        {
            try { 
            PushNotificationChannel channel = await PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();
            channel.Close();
            }
            catch (Exception exc)
            {
                Logger.WriteLine("Could not close pushnotificationchannel.");
                Logger.WriteLine(exc.Message);
            }
        }
    }
}
