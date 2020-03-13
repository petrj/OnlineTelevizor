using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using LoggerService;
using OnlineTelevizor.Models;
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

        public string DVBStreamerUrl
        {
            get
            {
                return GetSettingValue<string>("DVBStreamerUrl");
            }
            set
            {
                SaveSettingValue<string>("DVBStreamerUrl", value);
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
                return GetSettingValue<bool>("InternalPlayer");
            }
            set
            {
                SaveSettingValue<bool>("InternalPlayer", value);
            }
        }

        public bool AnimatedScrolling
        {
            get
            {
                return GetSettingValue<bool>("AnimatedScrolling");
            }
            set
            {
                SaveSettingValue< bool>("AnimatedScrolling", value);
            }
        }

        public bool DoNotSplitScreenOnLandscape
        {
            get
            {
                return GetSettingValue<bool>("DoNotSplitScreenOnLandscape");
            }
            set
            {
                SaveSettingValue<bool>("DoNotSplitScreenOnLandscape", value);
            }
        }

        public bool Fullscreen
        {
            get
            {
                return GetSettingValue<bool>("Fullscreen");
            }
            set
            {
                SaveSettingValue<bool>("Fullscreen", value);
            }
        }

        public bool PlayOnBackground
        {
            get
            {
                return GetSettingValue<bool>("PlayOnBackground");
            }
            set
            {
                SaveSettingValue<bool>("PlayOnBackground", value);
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

        public bool Purchased
        {
            get
            {
                return true;
            }
            set
            {

            }
        }

        public bool NotPurchased
        {
            get
            {
                return !Purchased;
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

        public string PurchaseProductId
        {
            get
            {
                return "onlinetelevizor.full";
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
    }
}
