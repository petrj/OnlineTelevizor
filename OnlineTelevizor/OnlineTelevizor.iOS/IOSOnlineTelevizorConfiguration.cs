using Foundation;
using LoggerService;
using OnlineTelevizor.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Xamarin.Forms;

namespace OnlineTelevizor.iOS
{
    public class IOSOnlineTelevizorConfiguration : IOnlineTelevizorConfiguration
    {
        private bool _debugMode = false;

        protected void SavePersistingSettingValue<T>(string key, T value)
        {
            try
            {
                if (typeof(T) == typeof(string))
                {
                    NSUserDefaults.StandardUserDefaults.SetString(value.ToString(), key);
                }
                if (typeof(T) == typeof(bool))
                {
                    NSUserDefaults.StandardUserDefaults.SetBool(Convert.ToBoolean(value), key);
                }
                if (typeof(T) == typeof(int))
                {
                    NSUserDefaults.StandardUserDefaults.SetInt(Convert.ToInt32(value), key);
                }

                //NSUserDefaults.StandardUserDefaults.Synchronize();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        protected T GetPersistingSettingValue<T>(string key)
        {
            T result = default(T);

            try
            {
                object val;

                if (typeof(T) == typeof(string))
                {
                    val = NSUserDefaults.StandardUserDefaults.StringForKey(key);
                }
                else
                if (typeof(T) == typeof(bool))
                {
                    val = NSUserDefaults.StandardUserDefaults.BoolForKey(key);
                }
                else
                if (typeof(T) == typeof(int))
                {
                    val = NSUserDefaults.StandardUserDefaults.IntForKey(key);
                }
                else
                {
                    val = default(T);
                }

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
                return GetPersistingSettingValue<string>("KUKIsn");
            }
            set
            {
                SavePersistingSettingValue<string>("KUKIsn", value);
            }
        }

        public string O2TVUsername
        {
            get
            {
                return GetPersistingSettingValue<string>("O2TVUsername");
            }
            set
            {
                SavePersistingSettingValue<string>("O2TVUsername", value);
            }
        }

        public string O2TVPassword
        {
            get
            {
                return GetPersistingSettingValue<string>("O2TVPassword");
            }
            set
            {
                SavePersistingSettingValue<string>("O2TVPassword", value);
            }
        }

        public string DVBStreamerUrl
        {
            get
            {
                return GetPersistingSettingValue<string>("DVBStreamerUrl");
            }
            set
            {
                SavePersistingSettingValue<string>("DVBStreamerUrl", value);
            }
        }

        public TVAPIEnum TVApi
        {
            get
            {
                var index = GetPersistingSettingValue<int>("TVApi");
                return (TVAPIEnum)index;
            }
            set
            {
                SavePersistingSettingValue<int>("TVApi", (int)value);
            }
        }

        public string Username
        {
            get
            {
                return GetPersistingSettingValue<string>("Username");
            }
            set
            {
                SavePersistingSettingValue<string>("Username", value);
            }
        }

        public string Password
        {
            get
            {
                return GetPersistingSettingValue<string>("Password");
            }
            set
            {
                SavePersistingSettingValue<string>("Password", value);
            }
        }

        public string ChildLockPIN
        {
            get
            {
                return GetPersistingSettingValue<string>("ChildLockPIN");
            }
            set
            {
                SavePersistingSettingValue<string>("ChildLockPIN", value);
            }
        }

        public bool ShowLocked
        {
            get
            {
                return GetPersistingSettingValue<bool>("ShowLocked");
            }
            set
            {
                SavePersistingSettingValue<bool>("ShowLocked", value);
            }
        }

        public bool ShowAdultChannels
        {
            get
            {
                var shw = GetPersistingSettingValue<bool>("ShowAdultChannels");
                return shw;
            }
            set
            {
                SavePersistingSettingValue<bool>("ShowAdultChannels", value);
            }
        }

        public bool InternalPlayer
        {
            get
            {
                return GetPersistingSettingValue<bool>("InternalPlayer");
            }
            set
            {
                SavePersistingSettingValue<bool>("InternalPlayer", value);
            }
        }

        public bool Fullscreen
        {
            get
            {
                return GetPersistingSettingValue<bool>("Fullscreen");
            }
            set
            {
                SavePersistingSettingValue<bool>("Fullscreen", value);
            }
        }

        public bool PlayOnBackground
        {
            get
            {
                return GetPersistingSettingValue<bool>("PlayOnBackground");
            }
            set
            {
                SavePersistingSettingValue<bool>("PlayOnBackground", value);
            }
        }

        public bool DoNotSplitScreenOnLandscape
        {
            get
            {
                return GetPersistingSettingValue<bool>("DoNotSplitScreenOnLandscape");
            }
            set
            {
                SavePersistingSettingValue<bool>("DoNotSplitScreenOnLandscape", value);
            }
        }

        public string LastChannelNumber
        {
            get
            {
                return GetPersistingSettingValue<string>("LastChannelNumber");
            }
            set
            {
                SavePersistingSettingValue<string>("LastChannelNumber", value);
            }
        }

        public string AutoPlayChannelNumber
        {
            get
            {
                var channelNumber = GetPersistingSettingValue<string>("AutoPlayChannelNumber");
                if (string.IsNullOrEmpty(channelNumber))
                    channelNumber = "-1"; // no autoplay

                return channelNumber;
            }
            set
            {
                SavePersistingSettingValue<string>("AutoPlayChannelNumber", value);
            }
        }

        public bool EnableLogging
        {
            get
            {
                return GetPersistingSettingValue<bool>("EnableLogging");
            }
            set
            {
                SavePersistingSettingValue<bool>("EnableLogging", value);
            }
        }

        public bool Purchased
        {
            get
            {
                return true;
            }
            set
            { }
        }

        public bool PurchaseTokenSent
        {
            get
            {
                return true;
            }
            set
            {

            }
        }

        public string PurchaseToken
        {
            get
            {
                return null;
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
                var index = GetPersistingSettingValue<int>("AppFontSize");
                return (AppFontSizeEnum)index;
            }
            set
            {
                SavePersistingSettingValue<int>("AppFontSize", (int)value);
            }
        }

        public LoggingLevelEnum LoggingLevel
        {
            get
            {
                var index = GetPersistingSettingValue<int>("LoggingLevel");
                if (index == 0)
                    index = 9; // default is error
                return (LoggingLevelEnum)index;
            }
            set
            {
                SavePersistingSettingValue<int>("LoggingLevel", (int)value);
            }
        }

        public string StreamQuality
        {
            get
            {
                return GetPersistingSettingValue<string>("StreamQuality");
            }
            set
            {
                SavePersistingSettingValue<string>("StreamQuality", value);
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
                return GetPersistingSettingValue<string>("ChannelGroup");
            }
            set
            {
                SavePersistingSettingValue<string>("ChannelGroup", value);
            }
        }

        public string ChannelFilterType
        {
            get
            {
                return GetPersistingSettingValue<string>("ChannelType");
            }
            set
            {
                SavePersistingSettingValue<string>("ChannelType", value);
            }
        }

        public string ChannelFilterName
        {
            get
            {
                return GetPersistingSettingValue<string>("ChannelFilterName");
            }
            set
            {
                SavePersistingSettingValue<string>("ChannelFilterName", value);
            }
        }

        public string DeviceId
        {
            get { return GetPersistingSettingValue<string>("DeviceId"); }
            set { SavePersistingSettingValue<string>("DeviceId", value); }
        }

        public string DevicePassword
        {
            get { return GetPersistingSettingValue<string>("DevicePassword"); }
            set { SavePersistingSettingValue<string>("DevicePassword", value); }
        }

        public string OutputDirectory
        {
            get
            {
                return null;
            }
        }

        public long UsableSpace
        {
            get
            {
                return -1;
            }
        }
    }
}
