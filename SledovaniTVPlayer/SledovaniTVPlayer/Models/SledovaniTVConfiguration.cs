using Android.Content;
using Android.Preferences;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Xamarin.Forms;

namespace SledovaniTVPlayer.Models
{
    public class SledovaniTVConfiguration : CustomSharedPreferencesObject, ISledovaniTVConfiguration
    {
        public SledovaniTVConfiguration(Context context) : base(context)
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
