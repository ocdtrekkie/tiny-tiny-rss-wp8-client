using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows;

namespace TinyTinyRSS.Interface.Classes
{
    public sealed class Headline : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public int id {get; set;}
        private bool _unread;
        private bool _marked;
        private string _title;
        private string _author;
        private long _updated;
        public bool unread
        {
            get { return _unread; }
            set
            {
                _unread = value;
                OnPropertyChanged("unread");
            }
        }
        public bool marked
        {
            get { return _marked; }
            set
            {
                _marked = value;
                OnPropertyChanged("marked");
            }
        }
        public bool published {get; set;}
        public long updated
        {
            get { return _updated; }
            set
            {
                _updated = value;
                OnPropertyChanged("formattedDate");
            }
        }
        public bool is_updated {get; set;}
        public string title
        {
            get { return _title; }
            set
            {
                _title = value;
                OnPropertyChanged("title");
            }
        }
        public string link {get; set;}
        public int? feed_id {get; set;}
        public object[] tags {get; set;}
        public object[] labels {get; set;}
        public string feed_title {get; set;}
        public int comments_count {get; set;}
        public string comments_link {get; set;}
        public bool always_display_attachments {get; set;}
        public string author
        {
            get { return _author; }
            set
            {
                _author = value;
                OnPropertyChanged("author");
            }
        }
        public int score { get; set; }

        // Create the OnPropertyChanged method to raise the event 
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        public string formattedDate
        {
            get
            {
                if (updated > 0)
                {
                    DateTime updatedDate = TimeReturnUnix2DateUtc(_updated).ToLocalTime();
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
