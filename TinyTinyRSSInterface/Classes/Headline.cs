using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace TinyTinyRSS.Interface.Classes
{
    public class Headline
    {
        public int id {get; set;}
        public bool unread {get; set;}
        public bool marked {get; set;}
        public bool published {get; set;}
        public long updated {get; set;} //timestamp
        public bool is_updated {get; set;}
        public string title {get; set;}
        public string link {get; set;}
        public int? feed_id {get; set;}
        public object[] tags {get; set;}
        public object[] labels {get; set;}
        public string feed_title {get; set;}
        public int comments_count {get; set;}
        public string comments_link {get; set;}
        public bool always_display_attachments {get; set;}
        public string author {get; set;}
        public int score { get; set; }

        public string formattedDate
        {
            get
            {
                if (updated > 0)
                {
                    DateTime updatedDate = TimeReturnUnix2DateUtc(updated).ToLocalTime();
                    string monthDay = updatedDate.ToString(CultureInfo.CurrentCulture.DateTimeFormat.MonthDayPattern, CultureInfo.CurrentCulture);
                    string fullMonth = CultureInfo.CurrentCulture.DateTimeFormat.MonthNames[updatedDate.Month-1];
                    string shortMonth = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(updatedDate.Month);
                    return monthDay.Replace(fullMonth, shortMonth) + " " + updatedDate.ToString("t", CultureInfo.CurrentCulture);
                }
                else
                {
                    return "";
                }
            }
        }

        public override string ToString()
        {
            return "Artikel: \"" + title + "\" von " + author + " aus Feed '" + feed_title + "' ("+ TimeReturnUnix2DateUtc(updated).ToLocalTime().ToString() + ").";
        }

        internal static DateTime TimeReturnUnix2DateUtc(long utime)
        {
            // Erstellen des Zeitstempel für UNIX Zeit
            var universalTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            // Rückgabe des DateTime Objektes
            return universalTime.AddSeconds(utime);
        }
    }
}
