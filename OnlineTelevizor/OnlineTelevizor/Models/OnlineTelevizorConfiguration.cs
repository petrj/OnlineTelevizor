using Android.Content;
using Android.Preferences;
using LoggerService;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Xamarin.Forms;

namespace OnlineTelevizor.Models
{
    public class OnlineTelevizorConfiguration : CustomSharedPreferencesObject, IOnlineTelevizorConfiguration
    {
        private bool _debugMode = false;

        public OnlineTelevizorConfiguration(Context context) : base(context)
        {}

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
                return GetPersistingSettingValue<bool>("Purchased");
            }
            set
            {
                SavePersistingSettingValue<bool>("Purchased", value);
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
    }
}
