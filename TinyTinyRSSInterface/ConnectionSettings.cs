using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace TinyTinyRSSInterface
{
    public class ConnectionSettings
    {
        //Keys for settings
        public static string _markReadKey = "AutoMarkRead";
        public static string _serverKey = "Server";
        public static string _usernameKey = "Username";
        public static string _passwordKey = "Password";
        public static string _showUnreadOnlyKey = "ShowUnreadOnly";
        public static string _logExistsKey = "ErrorLogExists";

        private static ConnectionSettings instance;
        private string _server;
        private string _username;
        private string _password;
        private string _markRead;
        private string _unreadOnly;
        private string _logExists;

        private ConnectionSettings()
        {
        }

        public static ConnectionSettings getInstance() {
            if (instance == null)
            {
                instance = new ConnectionSettings();
            }
            return instance;
        }

        public string server
        {
            get
            {
                if (_server == null)
                {
                  _server = ReadSetting(_serverKey);
                } 
                return _server;
            }
            set
            {
                SaveSetting(_serverKey, value);
                _server = value;
            }
        }

        public string username
        {
            get
            {
                if (_username == null)
                {
                    _username = ReadSetting(_usernameKey);
                } 
                return _username;
            }
            set
            {
                SaveSetting(_usernameKey, value);
                _username = value;
            }
        }

        public string password
        {
            get
            {
                if (_password == null)
                {
                    if (IsolatedStorageSettings.ApplicationSettings.Contains(_passwordKey))
                    {
                        var bytes = IsolatedStorageSettings.ApplicationSettings[_passwordKey] as byte[];
                        var unEncrypteBytes = ProtectedData.Unprotect(bytes, null);
                        return Encoding.UTF8.GetString(unEncrypteBytes, 0, unEncrypteBytes.Length);
                    }
                    else
                    {
                        return string.Empty;
                    }
                }
                return _password;
            }
            set
            {
                var encryptedBytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(value), null);
                IsolatedStorageSettings.ApplicationSettings[_passwordKey] = encryptedBytes;
                _password = value;
            }
        }

        public bool markRead
        {
            get
            {
                if (_markRead == null)
                {
                    _markRead = ReadSetting(_markReadKey);
                } 
                return _markRead.ToLower().Equals("true");
            }
            set
            {
                SaveSetting(_markReadKey, value.ToString());
                _markRead = value.ToString();
            }
        }

        public bool logExists
        {
            get
            {
                if (_logExists == null)
                {
                    _logExists = ReadSetting(_logExistsKey);
                }
                return _logExists.ToLower().Equals("true");
            }
            set
            {
                SaveSetting(_logExistsKey, value.ToString());
                _logExists = value.ToString();
            }
        }

        public bool showUnreadOnly
        {
            get
            {
                if (_unreadOnly == null)
                {
                    _unreadOnly = ReadSetting(_showUnreadOnlyKey);
                }
                return _unreadOnly.ToLower().Equals("true");
            }
            set
            {
                SaveSetting(_showUnreadOnlyKey, value.ToString());
                _unreadOnly = value.ToString();
            }
        }

        private static void SaveSetting(string key, string value)
        {
            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
            if (!settings.Contains(key))
            {
                settings.Add(key, value);
            }
            else
            {
                settings[key] = value;
            }
            settings.Save();
        }

        private string ReadSetting(string key)
        {
            if (IsolatedStorageSettings.ApplicationSettings.Contains(key))
            {
                return IsolatedStorageSettings.ApplicationSettings[key] as string;
            }
            return "";
        }
    }
}