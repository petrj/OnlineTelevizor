using Android.Content;
using Android.Preferences;
using LoggerService;
using Newtonsoft.Json.Linq;
using OnlineTelevizor.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using Xamarin.Forms;

namespace OnlineTelevizor.Droid
{
    public class AndroidOnlineTelevizorConfiguration : CustomSharedPreferencesObject, IOnlineTelevizorConfiguration
    {
        private bool _debugMode = false;

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
                return !ExternalPlayer;
            }
            set
            {
                ExternalPlayer = !value;
            }
        }

        private bool ExternalPlayer
        {
            get
            {
                return GetPersistingSettingValue<bool>("ExternalPlayer");
            }
            set
            {
                SavePersistingSettingValue<bool>("ExternalPlayer", value);
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
                return GetPersistingSettingValue<bool>("Purchased");
            }
            set
            {
                SavePersistingSettingValue<bool>("Purchased", value);
            }
        }


        public bool PurchaseTokenSent
        {
            get
            {
                return GetPersistingSettingValue<bool>("PurchaseTokenSent");
            }
            set
            {
                SavePersistingSettingValue<bool>("PurchaseTokenSent", value);
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

        private static string DownloadDataAsString(string url)
        {
            string result = string.Empty;

            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Timeout = 500;
            request.ReadWriteTimeout = 500;
            var wresp = (HttpWebResponse)request.GetResponse();

            using (var stream = wresp.GetResponseStream())
            {
                using (var sr = new StreamReader(stream, Encoding.UTF8))
                {
                    string line = null;
                    while ((line = sr.ReadLine()) != null)
                    {
                        result += line;
                    }
                    sr.Close();
                }
                stream.Close();
            }

            return result;
        }

        private static T GetTypedJObject<T>(JObject obj, string paramName)
        {
            if (obj.TryGetValue(paramName, out var token))
            {
                return obj.SelectToken(paramName).Value<T>();
            }

            return default(T);
        }

        private bool EmptyCredentials
        {
            get
            {
                return
                    String.IsNullOrEmpty(Username) &&
                    String.IsNullOrEmpty(Password) &&
                    String.IsNullOrEmpty(KUKIsn) &&
                    String.IsNullOrEmpty(O2TVUsername) &&
                    String.IsNullOrEmpty(O2TVPassword);
            }
        }

        public string OutputDirectory
        {
            get
            {
                try
                {
                    // internal storage - always writable directory
                    try
                    {
                        var pathToExternalMediaDirs = Android.App.Application.Context.GetExternalMediaDirs();

                        if (pathToExternalMediaDirs.Length == 0)
                            throw new DirectoryNotFoundException();

                        return pathToExternalMediaDirs[0].AbsolutePath;
                    }
                    catch
                    {
                        // fallback for older API:

                        var internalStorageDir = Android.App.Application.Context.GetExternalFilesDir(Environment.SpecialFolder.MyDocuments.ToString());

                        return internalStorageDir.AbsolutePath;
                    }
                }
                catch
                {
                    var dir = Android.App.Application.Context.GetExternalFilesDir("");

                    return dir.AbsolutePath;
                }
            }
        }

        public bool? LoadCredentails(string url)
        {
            try
            {
                if (!EmptyCredentials)
                    return null;

                string credentials = DownloadDataAsString(url);

                var credentialsJson = JObject.Parse(credentials);

                if (credentialsJson.TryGetValue("SledovaniTV", out var sledovaniTVToken))
                {
                    Username = GetTypedJObject<string>(sledovaniTVToken as JObject, "username");
                    Password = GetTypedJObject<string>(sledovaniTVToken as JObject, "password");
                }

                if (credentialsJson.TryGetValue("O2TV", out var O2TVToken))
                {
                    O2TVUsername = GetTypedJObject<string>(O2TVToken as JObject, "username");
                    O2TVPassword = GetTypedJObject<string>(O2TVToken as JObject, "password");
                }

                if (credentialsJson.TryGetValue("KUKI", out var KUKIToken))
                {
                    KUKIsn = GetTypedJObject<string>(KUKIToken as JObject, "KUKIsn");
                }

                if (credentialsJson.GetValue("internalPlayer") != null)
                    InternalPlayer = GetTypedJObject<bool>(credentialsJson, "internalPlayer");

                if (credentialsJson.GetValue("playOnBackground") != null)
                    PlayOnBackground = GetTypedJObject<bool>(credentialsJson, "playOnBackground");

                if (credentialsJson.GetValue("fullscreen") != null)
                    Fullscreen = GetTypedJObject<bool>(credentialsJson, "fullscreen");

                if (credentialsJson.GetValue("purchased") != null)
                    Purchased = GetTypedJObject<bool>(credentialsJson, "purchased");

                var TVAPIAsString = GetTypedJObject<string>(credentialsJson, "TVAPI");
                if ((!string.IsNullOrEmpty(TVAPIAsString)) && (Enum.TryParse(typeof(TVAPIEnum), TVAPIAsString, out var api)))
                {
                    TVApi = (TVAPIEnum)api;
                }

                var fontSizeAsString = GetTypedJObject<string>(credentialsJson, "fontSize");
                if ((!string.IsNullOrEmpty(fontSizeAsString)) && (Enum.TryParse(typeof(AppFontSizeEnum), fontSizeAsString, out var fs)))
                {
                    AppFontSize = (AppFontSizeEnum)fs;
                }

                return true;

            } catch (Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }
        }
    }
}
