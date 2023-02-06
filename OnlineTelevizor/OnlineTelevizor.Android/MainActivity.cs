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
using System.Threading.Tasks;
using Android.Support.V4.App;
using Android.Support.V4.View;
using Android.Content.Res;

namespace OnlineTelevizor.Droid
{
    [Activity(Label = "OnlineTelevizor", Icon = "@drawable/Icon", Theme = "@style/MainTheme", MainLauncher = true, Banner = "@drawable/banner", ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    [IntentFilter(new[] { Intent.ActionMain }, Categories = new[] { Intent.CategoryLauncher, Intent.CategoryLeanbackLauncher })]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity, InputManager.IInputDeviceListener
    {
        private App _app;
        private AndroidOnlineTelevizorConfiguration _cfg;

        private int _defaultUiOptions;
        private int _fullscreenUiOptions;
        protected ILoggingService _loggingService;
        NotificationHelper _notificationHelper;
        private static Android.Widget.Toast _instance;
        private DateTime _lastOnGenericMotionEventTime = DateTime.MinValue;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(savedInstanceState);

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            Plugin.CurrentActivity.CrossCurrentActivity.Current.Init(this, savedInstanceState);

            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);

            FFImageLoading.Forms.Platform.CachedImageRenderer.Init(enableFastRenderer: true);

            var context = Platform.AppContext;
            var activity = Platform.CurrentActivity;

            _cfg = new AndroidOnlineTelevizorConfiguration();

#if LOADCONFIG
            var loaded = _cfg.LoadCredentails("OnlineTelevizor.configuration.json", true);
            if (loaded != null && loaded.Value)
            {
                    ShowToastMessage("Configuration automatically loaded");
            }
#endif

#if LOGGING
            /*
            var fileLoggingService = new FileLoggingService(LoggingLevelEnum.Info);
            fileLoggingService.LogFilename = System.IO.Path.Join(_cfg.OutputDirectory, $"OnlineTelevizor-{DateTime.Now.ToString("yyyy-MM-dd")}.log");
            fileLoggingService.WriteToOutput = true;

            _loggingService = fileLoggingService;
            */

            _loggingService = new NLogLoggingService(GetType().Assembly, "OnlineTelevizor.Droid");

            _loggingService.Info("Starting activity");
#else
            _loggingService = new DummyLoggingService();
#endif

            try
            {
                string version = String.Empty;
                try
                {
                    version = VersionTracking.CurrentBuild;
                }
                catch {}

                var uiModeManager = (UiModeManager)GetSystemService(UiModeService);
                if (uiModeManager.CurrentModeType == UiMode.TypeTelevision)
                {
                    _cfg.IsRunningOnTV = true;
                }

                _loggingService.Info($"_cfg.IsRunningOnTV: {_cfg.IsRunningOnTV}");

                _app = new App(_cfg, _loggingService);
                _app.AppVersion = version;

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

                _notificationHelper = new NotificationHelper(this);

                if (!ServiceStarted(typeof(OnlineTelevizorService)))
                {
                    StartService(new Intent(this, typeof(OnlineTelevizorService)));
                }

                SubscribeMessages();

                var input_manager = (InputManager)GetSystemService(Context.InputService);

                LoadApplication(_app);
            } catch (Exception ex)
            {
                _loggingService.Error(ex, "Application start failed");
            }
        }

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            _loggingService.Error(e.Exception, "TaskScheduler_UnobservedTaskException");
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            _loggingService.Error(e.ExceptionObject as Exception, "CurrentDomain_UnhandledException");
        }

        private bool ServiceStarted(Type serviceType)
        {
            var manager = (ActivityManager)Android.App.Application.Context.GetSystemService(Context.ActivityService);
            foreach (var service in manager.GetRunningServices(10))
            {
                if (service.GetType() == serviceType)
                    return true;
            }

            return false;
        }

        private void SubscribeMessages()
        {
            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_ToastMessage, (message) =>
            {
                ShowToastMessage(message);
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_ShareUrl, (url) =>
            {
                var intent = new Intent(Intent.ActionView);
                var uri = Android.Net.Uri.Parse(url);
                intent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTask);
                Android.App.Application.Context.StartActivity(intent);
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_UriMessage, (url) =>
            {
                var intent = new Intent(Intent.ActionView);
                var uri = Android.Net.Uri.Parse(url);
                intent.SetDataAndType(uri, "video/*");
                intent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTask); // necessary for Android 5
                Android.App.Application.Context.StartActivity(intent);
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_EnableFullScreen, (msg) =>
            {
                SetFullScreen(true);
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_DisableFullScreen, (msg) =>
            {
                SetFullScreen(false);
            });

            MessagingCenter.Subscribe<SettingsPage>(this, BaseViewModel.MSG_CheckBatterySettings, (sender) =>
            {
                try
                {
                    var pm = (PowerManager)Android.App.Application.Context.GetSystemService(Context.PowerService);
                    bool ignoring = pm.IsIgnoringBatteryOptimizations("net.petrjanousek.OnlineTelevizor");

                    if (!ignoring)
                    {
                        MessagingCenter.Send<string>(string.Empty, BaseViewModel.MSG_RequestBatterySettings);
                    }
                }
                catch (Exception ex)
                {
                    _loggingService.Error(ex);
                }
            });

            MessagingCenter.Subscribe<SettingsPage>(this, BaseViewModel.MSG_SetBatterySettings, (sender) =>
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

            MessagingCenter.Subscribe<MainPage, ChannelItem>(this, BaseViewModel.MSG_PlayInternalNotification, (sender, channel) =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    Task.Run(async () => await ShowPlayingNotification(channel));
                });
            });

            MessagingCenter.Subscribe<MainPageViewModel, ChannelItem>(this, BaseViewModel.MSG_UpdateInternalNotification, (sender, channel) =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    Task.Run(async () => await ShowPlayingNotification(channel));
                });
            });

            MessagingCenter.Subscribe<string>(string.Empty, BaseViewModel.MSG_StopPlayInternalNotification, (sender) =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    StopPlayingNotification();
                });
            });

            MessagingCenter.Subscribe<BaseViewModel, ChannelItem>(this, BaseViewModel.MSG_RecordNotificationMessage, (sender, channel) =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    Task.Run(async () => await ShowRecordNotification(channel));
                });
            });

            MessagingCenter.Subscribe<BaseViewModel, ChannelItem>(this, BaseViewModel.MSG_UpdateRecordNotificationMessage, (sender, channel) =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    Task.Run(async () => await ShowRecordNotification(channel));
                });
            });

            MessagingCenter.Subscribe<string>(string.Empty, BaseViewModel.MSG_StopRecordNotificationMessage, (sender) =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    StopRecordNotification();
                });
            });

            MessagingCenter.Subscribe<string>(string.Empty, BaseViewModel.MSG_StopPlayInternalNotificationAndQuit, (sender) =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    StopPlayingNotification();
                    Android.OS.Process.KillProcess(Android.OS.Process.MyPid());
                });
            });
        }

        protected override void OnDestroy()
        {
            _loggingService.Info("Activity destroyed");

            base.OnDestroy();
        }

        protected override void OnPostResume()
        {
            _loggingService.Info("Activity OnPostResume");
            base.OnPostResume();
        }

        protected override void OnResume()
        {
            _loggingService.Info("Activity OnResume");
            base.OnResume();
        }

        protected override void OnStart()
        {
            _loggingService.Info("Activity OnStart");
            base.OnStart();
        }

        protected override void OnStop()
        {
            _loggingService.Info("Activity OnStop");
            base.OnStop();
        }

        private async Task ShowPlayingNotification(ChannelItem channel)
        {
            try
            {
                _loggingService.Info("ShowPlayingNotification");

                string subTitle = String.Empty;
                string detail = String.Empty;
                if (channel.CurrentEPGItem != null)
                {
                    subTitle = channel.CurrentEPGItem.Title;
                    detail = channel.CurrentEPGItem.Start.ToString("HH:mm")
                        + " - " +
                      channel.CurrentEPGItem.Finish.ToString("HH:mm");
                }

                _notificationHelper.ShowPlayNotification(1, channel.Name, subTitle, detail);
            } catch (Exception ex)
            {
                _loggingService.Error(ex);
            }
        }

        private async Task ShowRecordNotification(ChannelItem channel)
        {
            try
            {
                _loggingService.Info("ShowRecordNotification");

                string subTitle = String.Empty;
                string detail = "Probíhá nahrávání";
                if (channel.CurrentEPGItem != null)
                {
                    subTitle = channel.CurrentEPGItem.Title;
                }

                Device.BeginInvokeOnMainThread(() =>
                {
                    _notificationHelper.ShowRecordNotification(2, channel.Name, subTitle, detail);
                });
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
            }
        }

        private void StopPlayingNotification()
        {
            try
            {
                _notificationHelper.CloseNotification(1);
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
            }
        }

        private void StopRecordNotification()
        {
            try
            {
                _notificationHelper.CloseNotification(2);
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
            }
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
            _loggingService.Debug($"OnKeyDown: keyCode:{keyCode}, long:{e.IsLongPress}");

            if (e.IsLongPress)
            {
                MessagingCenter.Send(keyCode.ToString(), BaseViewModel.MSG_KeyLongMessage);
            }

            MessagingCenter.Send(keyCode.ToString(), BaseViewModel.MSG_KeyMessage);

            return base.OnKeyDown(keyCode, e);
        }

        public override bool OnGenericMotionEvent(MotionEvent e)
        {
            // https://github.com/xamarin/monodroid-samples/blob/main/tv/VisualGameController/VisualGameController/FullScreenActivity.cs

            try
            {
                if (_lastOnGenericMotionEventTime > DateTime.MinValue &&
                    (DateTime.Now- _lastOnGenericMotionEventTime).TotalMilliseconds < 100)
                {
                    return false;
                }

                _lastOnGenericMotionEventTime = DateTime.Now;

                _loggingService.Debug($"OnGenericMotionEvent: DownTime: {e.DownTime}");

                int sources = (int)e.Device.Sources;

                if (((sources & (int)InputSourceType.Gamepad) == (int)InputSourceType.Gamepad) ||
                    ((sources & (int)InputSourceType.Joystick) == (int)InputSourceType.Joystick))
                {
                    _loggingService.Debug($"OnGenericMotionEvent: Gamepad/Joystick action");
                    _loggingService.Debug($"       action : {e.Action.ToString()}");

                    var x = e.GetAxisValue(Axis.X);
                    var y = e.GetAxisValue(Axis.X);

                    var x1 = e.GetAxisValue(Axis.Z);
                    var y1 = e.GetAxisValue(Axis.Rz);

                    if (x>0.5 || x1 > 0.5)
                    {
                        MessagingCenter.Send("Right", BaseViewModel.MSG_KeyMessage);
                        return true;
                    }

                    if (x < -0.5 || x1 < -0.5)
                    {
                        MessagingCenter.Send("Left", BaseViewModel.MSG_KeyMessage);
                        return true;
                    }

                    if (y > 0.5 || y1 > 0.5)
                    {
                        MessagingCenter.Send("Down", BaseViewModel.MSG_KeyMessage);
                        return true;
                    }

                    if (y < -0.5 || y1 < -0.5)
                    {
                        MessagingCenter.Send("Up", BaseViewModel.MSG_KeyMessage);
                        return true;
                    }
                }

            } catch (Exception ex)
            {
                _loggingService.Error(ex);
            }

            return base.OnGenericMotionEvent(e);
        }

        private void ShowToastMessage(string message)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    _instance?.Cancel();
                    _instance = Android.Widget.Toast.MakeText(Android.App.Application.Context, message, ToastLength.Short);

                    TextView textView;
                    Snackbar snackBar = null;

                    var tView = _instance.View;
                    if (tView == null)
                    {
                        // Since Android 11, custom toast is deprecated - using snackbar instead:

                        Activity activity = CrossCurrentActivity.Current.Activity;
                        var view = activity.FindViewById(Android.Resource.Id.Content);

                        snackBar = Snackbar.Make(view, message, Snackbar.LengthLong);

                        textView = snackBar.View.FindViewById<TextView>(Resource.Id.snackbar_text);
                    }
                    else
                    {
                        // using Toast

                        tView.Background.SetColorFilter(Android.Graphics.Color.Gray, PorterDuff.Mode.SrcIn); //Gets the actual oval background of the Toast then sets the color filter
                        textView = (TextView)tView.FindViewById(Android.Resource.Id.Message);
                        textView.SetTypeface(Typeface.DefaultBold, TypefaceStyle.Bold);
                    }

                    var minTextSize = textView.TextSize; // 16

                    textView.SetTextColor(Android.Graphics.Color.White);

                    var screenHeightRate = 0;

                        //configuration font size:

                        //Normal = 0,
                        //AboveNormal = 1,
                        //Big = 2,
                        //Biger = 3,
                        //VeryBig = 4,
                        //Huge = 5

                    if (DeviceDisplay.MainDisplayInfo.Height < DeviceDisplay.MainDisplayInfo.Width)
                    {
                        // Landscape

                        screenHeightRate = Convert.ToInt32(DeviceDisplay.MainDisplayInfo.Height / 16.0);
                        textView.SetMaxLines(5);
                    }
                    else
                    {
                        // Portrait

                        screenHeightRate = Convert.ToInt32(DeviceDisplay.MainDisplayInfo.Height / 32.0);
                        textView.SetMaxLines(5);
                    }

                    var fontSizeRange = screenHeightRate - minTextSize;
                    var fontSizePerValue = fontSizeRange / 5;

                    var fontSize = minTextSize + (int)_cfg.AppFontSize * fontSizePerValue;

                    textView.SetTextSize(Android.Util.ComplexUnitType.Px, Convert.ToSingle(fontSize));

                    if (snackBar != null)
                    {
                        snackBar.Show();
                    }
                    else
                    {
                        _instance.Show();
                    }
                }
                catch (Exception ex)
                {
                    _loggingService.Error(ex);
                }
            });
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