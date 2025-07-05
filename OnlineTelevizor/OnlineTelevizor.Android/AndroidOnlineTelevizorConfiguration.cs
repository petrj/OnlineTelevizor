using Android.Content;
using Android.OS.Storage;
using Android.Preferences;
using Android.Provider;
using Android.Support.Transitions;
using Java.Util;
using LibVLCSharp.Shared;
using LoggerService;
using Newtonsoft.Json.Linq;
using OnlineTelevizor.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.Remoting.Contexts;
using System.Text;
using TVAPI;
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

        public bool InternalPlayerSwitchEnabled
        {
            get
            {
                return true;
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

        public bool FullscreenSwitchEnabled
        {
            get
            {
                return true;
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

        public bool PlayOnBackgroundSwitchEnabled
        {
            get
            {
                return true;
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

        public bool EmptyCredentials
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

        private string InternalStorageDirectory
        {
            get
            {
                // internal storage :

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

        public string SDCardPath
        {
            get
            {
                return GetPersistingSettingValue<string>("SDCardPath");
            }
            set
            {
                SavePersistingSettingValue<string>("SDCardPath", value);
            }
        }

        public string SDCardPathUri
        {
            get
            {
                return GetPersistingSettingValue<string>("SDCardPathUri");
            }
            set
            {
                SavePersistingSettingValue<string>("SDCardPathUri", value);
            }
        }

        public string OutputDirectory
        {
            get
            {
                if (!WriteToSDCard)
                {
                    return InternalStorageDirectory;
                } else
                {
                    // external storage :

                    if (!String.IsNullOrEmpty(SDCardPath))
                    {
                        return SDCardPath;
                    }

                    var externalPath = "";

                    try
                    {
                        var context = Android.App.Application.Context;
                        var storageManager = (Android.OS.Storage.StorageManager)context.GetSystemService(Android.Content.Context.StorageService);

                        var volumeList = (Java.Lang.Object[])storageManager.Class.GetDeclaredMethod("getVolumeList").Invoke(storageManager);

                        var list = new List<string>();

                        foreach (var storage in volumeList)
                        {
                            if (storage is StorageVolume volume)
                            {
                                if (volume.IsPrimary || volume.IsEmulated || !volume.IsRemovable)
                                {
                                    continue;
                                }

                                // first external device
                                externalPath = volume.Directory.AbsolutePath;
                                break;
                            }
                        }
                    } catch (Exception ex)
                    {
                        // fallback for older API:

                        var dirs = Android.App.Application.Context.GetExternalFilesDirs(null);
                        foreach (var dir in dirs)
                        {
                            if (dir.ToString().StartsWith("/storage/emulated/"))
                            {
                                continue;
                            }

                            // first external device
                            externalPath = dir.ToString();
                            break;
                        }
                    }

                    //if (externalPath.EndsWith("files"))
                    //{
                    //    externalPath = externalPath.Substring(0, externalPath.Length - 5); // remove "files" from the end
                    //}

                    return externalPath;
                }
            }
        }

        public long UsableSpace
        {
            get
            {
                try
                {
                    return Android.OS.Environment.ExternalStorageDirectory.UsableSpace;
                } catch (Exception ex)
                {
                    return -1;
                }
            }
        }

        public List<string> FavouriteChannelNames
        {
            get
            {
                var favouriteChannelNamesAsString = GetPersistingSettingValue<string>("FavouriteChannelNames");

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

                SavePersistingSettingValue<string>("FavouriteChannelNames", favouriteChannelNamesAsString);
            }
        }

        public bool ShowOnlyFavouriteChannels
        {
            get
            {
                return GetPersistingSettingValue<bool>("ShowOnlyFavouriteChannels");
            }
            set
            {
                SavePersistingSettingValue<bool>("ShowOnlyFavouriteChannels", value);
            }
        }

        public bool WriteToSDCard
        {
            get
            {
                return GetPersistingSettingValue<bool>("WriteToSDCard");
            }
            set
            {
                SavePersistingSettingValue<bool>("WriteToSDCard", value);
            }
        }

        public string DefaultConfigurationFileName
        {
            get
            {
                return System.IO.Path.Join(InternalStorageDirectory, "OnlineTelevizor.configuration.json");
            }
        }

        public bool? TryLoadConfiguration()
        {
            try
            {
                string credentials = System.IO.File.ReadAllText(DefaultConfigurationFileName);

                var credentialsJson = JObject.Parse(credentials);

                if (credentialsJson.TryGetValue("SledovaniTV", out var sledovaniTVToken))
                {
                    Username = GetTypedJObject<string>(sledovaniTVToken as JObject, "username");
                    Password = GetTypedJObject<string>(sledovaniTVToken as JObject, "password");

                    DeviceId = GetTypedJObject<string>(sledovaniTVToken as JObject, "deviceid");
                    DevicePassword = GetTypedJObject<string>(sledovaniTVToken as JObject, "deviceauth");
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

                if (credentialsJson.TryGetValue("Demo", out var demoToken))
                {
                    DemoCustomChannelUrl = GetTypedJObject<string>(demoToken as JObject, "url");
                    DemoCustomChannelName = GetTypedJObject<string>(demoToken as JObject, "name");
                    DemoCustomChannelType = GetTypedJObject<string>(demoToken as JObject, "type");
                }

                return true;

            } catch (Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }
        }

        public bool IsRunningOnTV { get; set; } = false;

        public int RemoteAccessServicePort
        {
            get
            {
                var port = GetPersistingSettingValue<int>("RemoteAccessServicePort");
                if (port == default(int))
                {
                    port = 49152;
                }

                return port;
            }
            set
            {
                SavePersistingSettingValue<int>("RemoteAccessServicePort", value);
            }
        }

        public string RemoteAccessServiceSecurityKey
        {
            get
            {
                var key = GetPersistingSettingValue<string>("RemoteAccessServiceSecurityKey");
                if (key == default(string))
                {
                    key = "OnlineTelevizor";
                }

                return key;
            }
            set { SavePersistingSettingValue<string>("RemoteAccessServiceSecurityKey", value); }
        }

        public string RemoteAccessServiceIP
        {
            get
            {
                var ip = GetPersistingSettingValue<string>("RemoteAccessServiceIP");
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
            set { SavePersistingSettingValue<string>("RemoteAccessServiceIP", value); }
        }

        public bool AllowRemoteAccessService
        {
            get
            {
                return GetPersistingSettingValue<bool>("AllowRemoteAccessService");
            }
            set
            {
                SavePersistingSettingValue<bool>("AllowRemoteAccessService", value);
            }
        }

        public string DemoCustomChannelUrl
        {
            get { return GetPersistingSettingValue<string>("DemoCustomChannelUrl"); }
            set { SavePersistingSettingValue<string>("DemoCustomChannelUrl", value); }
        }

        public string DemoCustomChannelName
        {
            get { return GetPersistingSettingValue<string>("DemoCustomChannelName"); }
            set { SavePersistingSettingValue<string>("DemoCustomChannelName", value); }
        }

        public string DemoCustomChannelType
        {
            get { return GetPersistingSettingValue<string>("DemoCustomChannelType"); }
            set { SavePersistingSettingValue<string>("DemoCustomChannelType", value); }
        }
    }
}
