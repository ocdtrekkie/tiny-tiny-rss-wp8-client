﻿using System;
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
        public static string _sortOrderKey = "SortOrder";
        public static string _logExistsKey = "ErrorLogExists";
        public static string _progressBarAsCounterKey = "ProgressAsCntr";
        public static string _liveTileActivatedKey = "LiveTileActive";
        public static string _liveTileUpdateIntervalKey = "LiveTileUpdateInterval";
        public static string _channelUriKey = "LiveTileUpdateChannel";

        private static ConnectionSettings instance;
        private string _server;
        private string _username;
        private string _password;
        private string _markRead;
        private string _unreadOnly;
        private string _sortOrder;
        private string _logExists;
        private string _progressAsCntr;
        private string _liveTileActivated;
        private string _liveTileUpdateInterval;
        private string _liveTileChannelUri;

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

        public bool liveTileActive
        {
            get
            {
                if (_liveTileActivated == null)
                {
                    _liveTileActivated = ReadSetting(_liveTileActivatedKey);
                }
                return _liveTileActivated.ToLower().Equals("true");
            }
            set
            {
                SaveSetting(_liveTileActivatedKey, value.ToString());
                _liveTileActivated = value.ToString();
            }
        }

        public bool progressAsCntr
        {
            get
            {
                if (_progressAsCntr == null)
                {
                    _progressAsCntr = ReadSetting(_progressBarAsCounterKey);
                }
                return !_progressAsCntr.ToLower().Equals("false");
            }
            set
            {
                SaveSetting(_progressBarAsCounterKey, value.ToString());
                _progressAsCntr = value.ToString();
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

        public int sortOrder
        {
            get
            {
                if (_sortOrder == null)
                {
                    _sortOrder = ReadSetting(_sortOrderKey);
                    if (_sortOrder.Equals(""))
                        _sortOrder = "0";
                }
                return int.Parse(_sortOrder);
            }
            set
            {
                SaveSetting(_sortOrderKey, value.ToString());
                _sortOrder = value.ToString();
            }
        }

        public int tileUpdateInterval
        {
            get
            {
                if (_liveTileUpdateInterval == null)
                {
                    _liveTileUpdateInterval = ReadSetting(_liveTileUpdateIntervalKey);
                    if (_liveTileUpdateInterval.Equals(""))
                        _liveTileUpdateInterval = "0";
                }
                return int.Parse(_liveTileUpdateInterval);
            }
            set
            {
                SaveSetting(_liveTileUpdateIntervalKey, value.ToString());
                _liveTileUpdateInterval = value.ToString();
            }
        }

        public string channelUri
        {
            get
            {
                if (_liveTileChannelUri == null)
                {
                    _liveTileChannelUri = ReadSetting(_channelUriKey);
                }
                return _liveTileChannelUri;
            }
            set
            {
                SaveSetting(_channelUriKey, value);
                _liveTileChannelUri = value;
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