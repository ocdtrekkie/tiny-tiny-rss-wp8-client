using CaledosLab.Portable.Logging;
using NotificationsExtensions.BadgeContent;
using NotificationsExtensions.TileContent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using TinyTinyRSS.Interface;
using TinyTinyRSS.Interface.Classes;
using TinyTinyRSSInterface;
using Windows.Foundation.Metadata;
using Windows.Networking.PushNotifications;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;
using Windows.System.Profile;
using Windows.UI.Notifications;
using Windows.Web.Http;

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
            string deviceId = GetDeviceID();

            PushNotificationChannel channel = null;
            try
            {
                channel = await PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();
                if (channel.Uri == ConnectionSettings.getInstance().channelUri)
                {
                    return true;
                }
                HttpFormUrlEncodedContent httpFormUrlEncodedContent = new HttpFormUrlEncodedContent(new[] {
                        new KeyValuePair<string, string>("action", "updateChannel"),
                        new KeyValuePair<string, string>("deviceId", deviceId),
                        new KeyValuePair<string, string>("channel", channel.Uri),
                        new KeyValuePair<string, string>("hash", HASH)
                    });


                HttpClient httpClient = new HttpClient();
                HttpRequestMessage msg = new HttpRequestMessage(new HttpMethod("POST"), new Uri(SERVERURL));
                msg.Content = httpFormUrlEncodedContent;
                HttpResponseMessage httpresponse = await httpClient.SendRequestAsync(msg).AsTask();
                var responseString = await httpresponse.Content.ReadAsStringAsync();


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
            if (!ConnectionSettings.getInstance().liveTileActive)
            {
                return true;
            }
            string deviceId = GetDeviceID();

            PushNotificationChannel channel = null;
            try
            {
                channel = await PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();
                HttpFormUrlEncodedContent httpFormUrlEncodedContent = new HttpFormUrlEncodedContent(new[] { 
                        new KeyValuePair<string, string>("action", "updateUser"),
                        new KeyValuePair<string, string>("deviceId", deviceId),
                        new KeyValuePair<string, string>("loginName", user),
                        new KeyValuePair<string, string>("loginPassword", password),
                        new KeyValuePair<string, string>("server", server),
                        new KeyValuePair<string, string>("channel", channel.Uri),
                        new KeyValuePair<string, string>("hash", HASH)
                    });
                HttpClient httpClient = new HttpClient();
                HttpRequestMessage msg = new HttpRequestMessage(new HttpMethod("POST"), new Uri(SERVERURL));
                msg.Content = httpFormUrlEncodedContent;
                HttpResponseMessage httpresponse = await httpClient.SendRequestAsync(msg).AsTask();
                var responseString = await httpresponse.Content.ReadAsStringAsync();


                if (!responseString.Equals("1"))
                {
                    Logger.WriteLine(responseString);
                    return false;
                }
                else
                {
                    ConnectionSettings.getInstance().channelUri = channel.Uri;
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLine(ex.Message);
                return false;
            }
        }

        public static async Task ClosePushNotifications()
        {
            try
            {
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
                try
                {
                    fresh = await TtRssInterface.getInterface().getUnReadCount(true);
                }
                catch (TtRssException)
                {
                    fresh = 0;
                }
            }
            ITileSquare150x150IconWithBadge tileContent = TileContentFactory.CreateTileSquare150x150IconWithBadge();
            tileContent.ImageIcon.Src = "ms-appx:///Assets/LiveTile.png";
            TileUpdateManager.CreateTileUpdaterForApplication().Update(tileContent.CreateNotification());

            BadgeNumericNotificationContent badgeContent = new BadgeNumericNotificationContent((uint)fresh);
            BadgeUpdateManager.CreateBadgeUpdaterForApplication().Update(badgeContent.CreateNotification());

        }

        public static string GetDeviceID()
        {
            HardwareToken token = HardwareIdentification.GetPackageSpecificToken(null);
            IBuffer hardwareId = token.Id;

            HashAlgorithmProvider hasher = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Sha256);
            IBuffer hashed = hasher.HashData(hardwareId);

            string hashedString = CryptographicBuffer.EncodeToHexString(hashed);
            return hashedString;
        }
    }
}
