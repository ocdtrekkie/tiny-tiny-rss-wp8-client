using CaledosLab.Portable.Logging;
using NotificationsExtensions.BadgeContent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using TinyTinyRSS.Interface;
using Windows.UI.Notifications;

namespace TinyTinyRSS.Classes
{
    public class Helper
    {
        /// <summary>
        /// Appends a '+' symbol toa given string if the given bool is true.
        /// </summary>
        /// <param name="append">Should + be appended?</param>
        /// <param name="toAppendTo">Text to append + to.</param>
        /// <returns>Given string with or without +-sign.</returns>
        public static string AppendPlus(bool append, string toAppendTo)
        {
            if (append)
            {
                toAppendTo = toAppendTo + "+";
            }
            return toAppendTo;
        }

        /// <summary>
        /// Find an element with the given name below the given element.
        /// </summary>
        /// <param name="element">Element that should contain the other one.</param>
        /// <param name="name">Name to look for.</param>
        /// <returns>The found element or null.</returns>
        public static FrameworkElement FindDescendantByName(FrameworkElement element, string name)
        {
            if (element == null || string.IsNullOrWhiteSpace(name)) { return null; }

            if (name.Equals(element.Name, StringComparison.OrdinalIgnoreCase))
            {
                return element;
            }
            var childCount = VisualTreeHelper.GetChildrenCount(element);
            for (int i = 0; i < childCount; i++)
            {
                var result = FindDescendantByName((VisualTreeHelper.GetChild(element, i) as FrameworkElement), name);
                if (result != null) { return result; }
            }
            return null;
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
    }
}
