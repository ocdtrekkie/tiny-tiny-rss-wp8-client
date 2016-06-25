using System;
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

        private static LoggingSession getInstance() {
            if (instance == null)
            {
                try
                {
                    instance = new LoggingSession("Default");
                } catch(Exception)
                {
                    // whatsoever
                }
            }
            return instance;
        }

        public static void addChannel(LoggingChannel channel)
        {
            if (getInstance() != null)
            {
                try
                {
                    getInstance().AddLoggingChannel(channel);
                } catch(Exception)
                {

                }
            }
        }

        public static async Task Close()
        {
            if (getInstance() != null)
            {
                await Save();
                getInstance().Dispose();
                instance = null;
            }
        }

        public static async Task<StorageFile> Save()
        {
            if (getInstance() != null)
            {
                StorageFolder storage = ApplicationData.Current.LocalFolder;
                try
                {
                    StorageFile x = await getInstance().SaveToFileAsync(storage, "LogSession.etl");
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