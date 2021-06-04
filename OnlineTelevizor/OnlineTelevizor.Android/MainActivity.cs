using System;
using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using OnlineTelevizor.Services;
using OnlineTelevizor.Views;
using Plugin.Permissions;
using Plugin.InAppBilling;
using Xamarin.Forms;
using OnlineTelevizor.ViewModels;
using OnlineTelevizor.Models;
using Android.Content;
using Android.Graphics;
using AndroidX.Core.Graphics;
using Android.Hardware.Input;
using static Android.OS.PowerManager;
using Plugin.CurrentActivity;
using Android.Support.Design.Widget;
using LoggerService;
using Xamarin.Essentials;

namespace OnlineTelevizor.Droid
{
    [Activity(Label = "OnlineTelevizor", Icon = "@drawable/Icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity, InputManager.IInputDeviceListener
    {
        private App _app;
        private AndroidOnlineTelevizorConfiguration _cfg;

        private int _defaultUiOptions;
        private int _fullscreenUiOptions;
        protected ILoggingService _loggingService;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(savedInstanceState);

            Plugin.CurrentActivity.CrossCurrentActivity.Current.Init(this, savedInstanceState);            

            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);

            var context = Xamarin.Essentials.Platform.AppContext;
            var activity = Xamarin.Essentials.Platform.CurrentActivity;

            // wake lock - permision WAKE_LOCK required
            //var pmanager = (PowerManager)this.GetSystemService("power");
            //_wakeLock = pmanager.NewWakeLock(WakeLockFlags.Partial, "net.petrjanousek.OnlineTelevizor_wakelock");
            //_wakeLock.SetReferenceCounted(false);
            //_wakeLock.Acquire();
            //..
            //_wakeLock.Release();            

            _cfg = new AndroidOnlineTelevizorConfiguration();

            if (_cfg.EnableLogging)
            {
                _loggingService = new BasicLoggingService(_cfg.LoggingLevel);
            }
            else
            {
                _loggingService = new DummyLoggingService();
            }

            _app = new App(_cfg, _loggingService);


            MessagingCenter.Subscribe<string>(this, BaseViewModel.ToastMessage, (message) =>
            {
                ShowToastMessage(message);
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.UriMessage, (url) =>
            {
                var intent = new Intent(Intent.ActionView);
                var uri = Android.Net.Uri.Parse(url);
                intent.SetDataAndType(uri, "video/*");
                intent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTask); // necessary for Android 5
                Android.App.Application.Context.StartActivity(intent);
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.EnableFullScreen, (msg) =>
            {
                SetFullScreen(true);
            });
            MessagingCenter.Subscribe<string>(this, BaseViewModel.DisableFullScreen, (msg) =>
            {
                SetFullScreen(false);
            });

            MessagingCenter.Subscribe<SettingsPage>(this, BaseViewModel.CheckBatterySettings, (sender) =>
            {
                try
                {
                    var pm = (PowerManager)Android.App.Application.Context.GetSystemService(Context.PowerService);
                    bool ignoring = pm.IsIgnoringBatteryOptimizations("net.petrjanousek.OnlineTelevizor");

                    if (!ignoring)
                    {
                        MessagingCenter.Send<string>(string.Empty, BaseViewModel.RequestBatterySettings);
                    }
                } catch (Exception ex)
                {
                    _loggingService.Error(ex);
                }
            });

            MessagingCenter.Subscribe<SettingsPage>(this, BaseViewModel.SetBatterySettings, (sender) =>
            {
                try
                {
                    var intent = new Intent();
                    intent.SetAction(Android.Provider.Settings.ActionIgnoreBatteryOptimizationSettings);
                    intent.SetFlags(ActivityFlags.NewTask);
                    Android.App.Application.Context.StartActivity(intent);
                }
                catch (Exception ex)
                {
                    _loggingService.Error(ex);
                }
            });

            // prevent sleep:
            Window window = (Forms.Context as Activity).Window;
            window.AddFlags(WindowManagerFlags.KeepScreenOn);

            // https://stackoverflow.com/questions/39248138/how-to-hide-bottom-bar-of-android-back-home-in-xamarin-forms
            _defaultUiOptions = (int)Window.DecorView.SystemUiVisibility;

            _fullscreenUiOptions = _defaultUiOptions;
            _fullscreenUiOptions |= (int)SystemUiFlags.LowProfile;
            _fullscreenUiOptions |= (int)SystemUiFlags.Fullscreen;
            _fullscreenUiOptions |= (int)SystemUiFlags.HideNavigation;
            _fullscreenUiOptions |= (int)SystemUiFlags.ImmersiveSticky;

            if (_cfg.Fullscreen)
            {
                SetFullScreen(true);
            }

            LoadApplication(_app);
        }

        private void SetFullScreen(bool on)
        {
            try
            {
                if (on)
                {
                    Window.DecorView.SystemUiVisibility = (StatusBarVisibility)_fullscreenUiOptions;
                }
                else
                {
                    Window.DecorView.SystemUiVisibility = (StatusBarVisibility)_defaultUiOptions;
                };
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            PermissionsImplementation.Current.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Android.Content.Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);            
        }

        public override bool OnKeyDown(Keycode keyCode, KeyEvent e)
        {
            MessagingCenter.Send(keyCode.ToString(), BaseViewModel.KeyMessage);

            return base.OnKeyDown(keyCode, e);
        }

        private void ShowToastMessage(string message)
        {
            try
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    Activity activity = CrossCurrentActivity.Current.Activity;
                    var view = activity.FindViewById(Android.Resource.Id.Content);

                    var snackBar = Snackbar.Make(view, message, Snackbar.LengthLong);

                    var textView = snackBar.View.FindViewById<TextView>(Resource.Id.snackbar_text);

                    var minTextSize = textView.TextSize; // 16
                    
                    textView.SetTextColor(Android.Graphics.Color.White);

                    var screenHeightRate = 0;

                    /*
                            configuration font size:

                            Normal = 0,     
                            AboveNormal = 1,
                            Big = 2,
                            Biger = 3,
                            VeryBig = 4,
                            Huge = 5        

                          */

                    if (DeviceDisplay.MainDisplayInfo.Height < DeviceDisplay.MainDisplayInfo.Width)
                    {
                        // Landscape

                        screenHeightRate = Convert.ToInt32(DeviceDisplay.MainDisplayInfo.Height / 16.0);
                        textView.SetMaxLines(2);
                    }
                    else
                    {
                        // Portrait

                        screenHeightRate = Convert.ToInt32(DeviceDisplay.MainDisplayInfo.Height / 32.0);
                        textView.SetMaxLines(4);
                    }

                    var fontSizeRange = screenHeightRate - minTextSize;
                    var fontSizePerValue = fontSizeRange/5;

                    var fontSize = minTextSize + (int)_cfg.AppFontSize * fontSizePerValue;

                    textView.SetTextSize(Android.Util.ComplexUnitType.Px, Convert.ToSingle(fontSize));

                    snackBar.Show();
                });
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
            }
        }

        public void OnInputDeviceAdded(int deviceId)
        {

        }

        public void OnInputDeviceChanged(int deviceId)
        {

        }

        public void OnInputDeviceRemoved(int deviceId)
        {

        }
    }
}