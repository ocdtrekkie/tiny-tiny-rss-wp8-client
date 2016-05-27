using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Windows.Storage;

namespace TinyTinyRSS.Classes
{
    public class LogSession
    {       
        private static FileLoggingSession instance;

        private ConnectionSettings()
        {
        }

        public static FileLoggingSession getInstance() {
            if (instance == null)
            {
                instance = new FileLoggingSession(App.LOGSESSION);
            }
            return instance;
        }
    }
}