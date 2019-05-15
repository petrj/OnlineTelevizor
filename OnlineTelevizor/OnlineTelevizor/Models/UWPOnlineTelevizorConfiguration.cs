using System;
using System.Collections.Generic;
using System.Text;
using LoggerService;

namespace OnlineTelevizor.Models
{
    public class UWPOnlineTelevizorConfiguration : IOnlineTelevizorConfiguration
    {
        private string _username = null;
        private string _password = null;
        private string _pin = null;
        private string _lastChannelNumber = null;
        private string _autoPlayChannelNumber = null;        
        private bool _showLocked = false;
        private bool _showAdultChannels = false;
        private bool _debugMode = false;
        private bool _loggingEnable = false;
        private bool _purchased = false;
        private AppFontSizeEnum _appFontSize = AppFontSizeEnum.Normal;
        private LoggingLevelEnum _loggingLevel = LoggingLevelEnum.Debug;
        private string _streamQuality = null;
        private string _channelFilterGroup = null;
        private string _channelFilterType = null;
        private string _channelFilterName = null;

        private string _deviceId = null;
        private string _devicePassword = null;

        public string Username
        {
            get
            {
                return _username;
            }
            set
            {
                _username = value;
            }
        }

        public string Password
        {
            get
            {
                return _password;
            }
            set
            {
                _password = value;
            }
        }

        public string ChildLockPIN
        {
            get
            {
                return _pin;
            }
            set
            {
                _pin = value;
            }
        }

        public bool ShowLocked
        {
            get
            {
                return _showLocked;
            }
            set
            {
                _showLocked = value;
            }
        }

        public bool ShowAdultChannels
        {
            get
            {
                return _showAdultChannels;
            }
            set
            {
                _showAdultChannels = value;
            }
        }

        public string LastChannelNumber
        {
            get
            {
                return _lastChannelNumber;
            }
            set
            {
                _lastChannelNumber = value;
            }
        }

        public string AutoPlayChannelNumber
        {
            get
            {
                return _autoPlayChannelNumber;
            }
            set
            {
                _autoPlayChannelNumber = value;
            }
        }

        public bool EnableLogging
        {
            get
            {
                return _loggingEnable;
            }
            set
            {
                _loggingEnable = value;
            }
        }

        public bool Purchased
        {
            get
            {
                return _purchased;
            }
            set
            {                
                _purchased = value;
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
                return _appFontSize;
            }
            set
            {
                _appFontSize = value;
            }
        }

        public LoggingLevelEnum LoggingLevel
        {
            get
            {                
                return _loggingLevel;
            }
            set
            {
                _loggingLevel = value;
            }
        }

        public string StreamQuality
        {
            get
            {
                return _streamQuality;
            }
            set
            {
                _streamQuality = value;
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
                return _channelFilterGroup;
            }
            set
            {
                _channelFilterGroup = value;
            }
        }

        public string ChannelFilterType
        {
            get
            {
                return _channelFilterType;
            }
            set
            {
                _channelFilterType = value;
            }
        }

        public string ChannelFilterName
        {
            get
            {
                return _channelFilterName;
            }
            set
            {
                _channelFilterName = value;
            }
        }

        public string DeviceId
        {
            get { return _deviceId; }
            set { _deviceId = value; }
        }

        public string DevicePassword
        {
            get { return _devicePassword; }
            set { _devicePassword = value; }
        }
    }
}
