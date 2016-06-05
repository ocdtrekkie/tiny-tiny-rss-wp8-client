using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using TinyTinyRSS.Interface;
using Windows.Foundation.Diagnostics;
using Windows.Storage;

namespace TinyTinyRSS.Classes
{
    public class LogSession
    {
        private static LoggingSession instance = null;

        private LogSession()
        {
        }

        public static LoggingSession getInstance() {
            if (instance == null)
            {
                instance = new LoggingSession("Default");
            }
            return instance;
        }

        public static async void Close()
        {
            if (instance != null)
            {
                await Save();
                instance = null;
            }
        }

        public static async Task<StorageFile> Save()
        {
            if (instance != null)
            {
                StorageFolder storage = ApplicationData.Current.LocalFolder;
                try
                {
                    StorageFile x = await instance.SaveToFileAsync(storage, "LogSession.etl");
                    ConnectionSettings.getInstance().lastLog = x.Name;
                    return x;
                } catch (Exception e)
                {
                    string ex = e.Message;
                }
            }
            return null;
        }
    }
}