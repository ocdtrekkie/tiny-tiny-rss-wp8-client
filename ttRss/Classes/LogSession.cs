using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Windows.Storage;

namespace TinyTinyRSS.Classes
{
    public class LogSession
    {       
        private static LoggingSession instance;

        private ConnectionSettings()
        {
        }

        public static LoggingSession getInstance() {
            if (instance == null)
            {
                instance = new LoggingSession(App.LOGSESSION);
            }
            return instance;
        }
    }
}