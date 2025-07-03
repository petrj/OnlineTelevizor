using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using LoggerService;
using OnlineTelevizor.Models;
using TVAPI;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.Storage.Streams;

namespace OnlineTelevizor.UWP
{
    public class UWPOnlineTelevizorConfiguration : IOnlineTelevizorConfiguration
    {
        private bool _debugMode = false;

        private ApplicationDataContainer Settings
        {
            get
            {
                return Windows.Storage.ApplicationData.Current.LocalSettings;
            }
        }

        protected void SaveSettingValue<T>(string key, T value)
        {
            try
            {
                Settings.Values[key] = value;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        protected T GetSettingValue<T>(string key)
        {
            T result = default(T);

            try
            {
                object val;

                val = Settings.Values[key];

                if (val == null)
                    val = default(T);

                result = (T)Convert.ChangeType(val, typeof(T));

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            return result;
        }

        public string KUKIsn
        {
            get
            {
                return GetSettingValue<string>("KUKIsn");
            }
            set
            {
                SaveSettingValue<string>("KUKIsn", value);
            }
        }

        public string O2TVUsername
        {
            get
            {
                return GetSettingValue<string>("O2TVUsername");
            }
            set
            {
                SaveSettingValue<string>("O2TVUsername", value);
            }
        }

        public string O2TVPassword
        {
            get
            {
                return GetSettingValue<string>("O2TVPassword");
            }
            set
            {
                SaveSettingValue<string>("O2TVPassword", value);
            }
        }

        public TVAPIEnum TVApi
        {
            get
            {
                var index = GetSettingValue<int>("TVApi");
                return (TVAPIEnum)index;
            }
            set
            {
                SaveSettingValue<int>("TVApi", (int)value);
            }
        }

        public string Username
        {
            get
            {
                return GetSettingValue<string>("Username");
            }
            set
            {
                SaveSettingValue<string>("Username", value);
            }
        }

        public string Password
        {
            get
            {
                return GetSettingValue<string>("Password");
            }
            set
            {
                SaveSettingValue<string>("Password", value);
            }
        }

        public string ChildLockPIN
        {
            get
            {
                return GetSettingValue<string>("ChildLockPIN");
            }
            set
            {
                SaveSettingValue<string>("ChildLockPIN", value);
            }
        }

        public bool ShowLocked
        {
            get
            {
                return GetSettingValue<bool>("ShowLocked");
            }
            set
            {
                SaveSettingValue<bool>("ShowLocked", value);
            }
        }

        public bool ShowAdultChannels
        {
            get
            {
                var shw = GetSettingValue<bool>("ShowAdultChannels");
                return shw;
            }
            set
            {
                SaveSettingValue<bool>("ShowAdultChannels", value);
            }
        }

        public bool InternalPlayer
        {
            get
            {
                return false;
            }
            set { }
        }

        public bool InternalPlayerSwitchEnabled
        {
            get
            {
                return false;
            }
        }

        public bool FullscreenSwitchEnabled
        {
            get
            {
                return false;
            }
        }

        public bool Fullscreen
        {
            get
            {
                return false;
            }
            set { }
        }

        public bool PlayOnBackground
        {
            get
            {
                return false;
            }
            set { }
        }

        public bool PlayOnBackgroundSwitchEnabled
        {
            get
            {
                return false;
            }
        }

        public string LastChannelNumber
        {
            get
            {
                return GetSettingValue<string>("LastChannelNumber");
            }
            set
            {
                SaveSettingValue<string>("LastChannelNumber", value);
            }
        }

        public string AutoPlayChannelNumber
        {
            get
            {
                var channelNumber = GetSettingValue<string>("AutoPlayChannelNumber");
                if (string.IsNullOrEmpty(channelNumber))
                    channelNumber = "-1"; // no autoplay

                return channelNumber;
            }
            set
            {
                SaveSettingValue<string>("AutoPlayChannelNumber", value);
            }
        }

        public bool EnableLogging
        {
            get
            {
                return GetSettingValue<bool>("EnableLogging");
            }
            set
            {
                SaveSettingValue<bool>("EnableLogging", value);
            }
        }

        public bool DebugMode
        {
            get
            {
                return _debugMode;
            }
            set
            {
                _debugMode = value;
            }
        }

        public AppFontSizeEnum AppFontSize
        {
            get
            {
                var index = GetSettingValue<int>("AppFontSize");
                return (AppFontSizeEnum)index;
            }
            set
            {
                SaveSettingValue<int>("AppFontSize", (int)value);
            }
        }

        public LoggingLevelEnum LoggingLevel
        {
            get
            {
                var index = GetSettingValue<int>("LoggingLevel");
                if (index == 0)
                    index = 9; // default is error
                return (LoggingLevelEnum)index;
            }
            set
            {
                SaveSettingValue<int>("LoggingLevel", (int)value);
            }
        }

        public string StreamQuality
        {
            get
            {
                return GetSettingValue<string>("StreamQuality");
            }
            set
            {
                SaveSettingValue<string>("StreamQuality", value);
            }
        }

        public string ChannelFilterGroup
        {
            get
            {
                return GetSettingValue<string>("ChannelGroup");
            }
            set
            {
                SaveSettingValue<string>("ChannelGroup", value);
            }
        }

        public string ChannelFilterType
        {
            get
            {
                return GetSettingValue<string>("ChannelType");
            }
            set
            {
                SaveSettingValue<string>("ChannelType", value);
            }
        }

        public string ChannelFilterName
        {
            get
            {
                return GetSettingValue<string>("ChannelFilterName");
            }
            set
            {
                SaveSettingValue<string>("ChannelFilterName", value);
            }
        }

        public string DeviceId
        {
            get { return GetSettingValue<string>("DeviceId"); }
            set { SaveSettingValue<string>("DeviceId", value); }
        }

        public string DevicePassword
        {
            get { return GetSettingValue<string>("DevicePassword"); }
            set { SaveSettingValue<string>("DevicePassword", value); }
        }

        public string OutputDirectory
        {
            get
            {
                return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }
        }

        public List<string> FavouriteChannelNames
        {
            get
            {
                var favouriteChannelNamesAsString = GetSettingValue<string>("FavouriteChannelNames");

                var res = new List<string>();

                if (string.IsNullOrEmpty(favouriteChannelNamesAsString))
                    return res;

                res.AddRange(favouriteChannelNamesAsString.Split(';'));

                return res;
            }
            set
            {
                var favouriteChannelNamesAsString = String.Empty;

                foreach (var channelName in value)
                {
                    if (string.IsNullOrEmpty(channelName))
                        continue;

                    if (favouriteChannelNamesAsString != String.Empty)
                        favouriteChannelNamesAsString += ";";

                    favouriteChannelNamesAsString += channelName.Replace(";", ",");
                }

                SaveSettingValue<string>("FavouriteChannelNames", favouriteChannelNamesAsString);
            }
        }

        public bool ShowOnlyFavouriteChannels
        {
            get
            {
                return GetSettingValue<bool>("ShowOnlyFavouriteChannels");
            }
            set
            {
                SaveSettingValue<bool>("ShowOnlyFavouriteChannels", value);
            }
        }

        public long UsableSpace
        {
            get
            {
                var path = OutputDirectory;
                if (string.IsNullOrEmpty(path))
                    return -1;

                var dr = path.Substring(0,1).ToUpper();

                foreach (DriveInfo drive in DriveInfo.GetDrives())
                {
                    if (drive.Name.Substring(0, 1).ToUpper() == dr)
                    {
                        return drive.AvailableFreeSpace;
                    }
                }
                return -1;
            }
        }

        public bool IsRunningOnTV
        {
            get
            {
                return false;
            }
            set { }
        }

        public int RemoteAccessServicePort
        {
            get
            {
                var port = GetSettingValue<int>("RemoteAccessServicePort");
                if (port == default(int))
                {
                    port = 49152;
                }

                return port;
            }
            set
            {
                SaveSettingValue<int>("RemoteAccessServicePort", value);
            }
        }

        public string RemoteAccessServiceSecurityKey
        {
            get
            {
                var key = GetSettingValue<string>("RemoteAccessServiceSecurityKey");
                if (key == default(string))
                {
                    key = "OnlineTelevizor";
                }

                return key;
            }
            set { SaveSettingValue<string>("RemoteAccessServiceSecurityKey", value); }
        }

        public bool AllowRemoteAccessService
        {
            get
            {
                return GetSettingValue<bool>("AllowRemoteAccessService");
            }
            set
            {
                SaveSettingValue<bool>("AllowRemoteAccessService", value);
            }
        }

        public string RemoteAccessServiceIP
        {
            get
            {
                var ip = GetSettingValue<string>("RemoteAccessServiceIP");
                if (ip == default(string))
                {
                    try
                    {
                        var ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
                        ip = ipHostInfo.AddressList[0].ToString();
                    }
                    catch
                    {
                        ip = "192.168.1.10";
                    }
                }

                return ip;
            }
            set { SaveSettingValue<string>("RemoteAccessServiceIP", value); }
        }

        public string DemoCustomChannelUrl
        {
            get { return GetSettingValue<string>("DemoCustomChannelUrl"); }
            set { SaveSettingValue<string>("DemoCustomChannelUrl", value); }
        }

        public string DemoCustomChannelName
        {
            get { return GetSettingValue<string>("DemoCustomChannelName"); }
            set { SaveSettingValue<string>("DemoCustomChannelName", value); }
        }

        public string DemoCustomChannelType
        {
            get { return GetSettingValue<string>("DemoCustomChannelType"); }
            set { SaveSettingValue<string>("DemoCustomChannelType", value); }
        }
    }
}
