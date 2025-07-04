﻿using System;
using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using OnlineTelevizor.Services;
using OnlineTelevizor.Views;
using Plugin.Permissions;
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
using Android.InputMethodServices;
using AndroidX.ConstraintLayout.Core.Widgets.Analyzer;
using System.IO;
using System.Threading;
using Android.Support.V4.Content;
using Android;
using Android.Provider;
using static AndroidX.Activity.Result.Contract.ActivityResultContracts;

namespace OnlineTelevizor.Droid
{
    [Activity(Name = "net.petrjanousek.OnlineTelevizor.MainActivity", Label = "Online Televizor", Icon = "@drawable/icon", Banner = "@drawable/banner", Theme = "@style/MainTheme", MainLauncher = false, Exported = false, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    [IntentFilter(new[] { Intent.ActionView, Intent.ActionDefault }, Categories = new[] { Intent.CategoryDefault })]
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
        private bool _dispatchKeyEventEnabled = false;
        private DateTime _dispatchKeyEventEnabledAt = DateTime.MaxValue;

        // just a random numbers
        const int RequestStorageId = 111;
        const int RequestFolderAccessId = 2000;

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
                if (_cfg.EmptyCredentials && System.IO.File.Exists(_cfg.DefaultConfigurationFileName))
                {
                    var loaded = _cfg.TryLoadConfiguration();
                    if (loaded != null && loaded.Value)
                    {
                        ShowToastMessage("Konfigurace byla načtena ze souboru");
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
            }

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

                RestorePersistedPermission();

                LoadApplication(_app);
            } catch (Exception ex)
            {
                _loggingService.Error(ex, "Application start failed");
            }
        }


        private void LaunchFolderPicker()
        {
            try
            {
                var intent = new Intent(Intent.ActionOpenDocumentTree);
                intent.AddFlags(ActivityFlags.GrantPersistableUriPermission);
                intent.AddFlags(ActivityFlags.GrantReadUriPermission);
                intent.AddFlags(ActivityFlags.GrantWriteUriPermission);
                StartActivityForResult(intent, RequestFolderAccessId);
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "LaunchFolderPicker failed");
            }
        }

        private void RequestStoragePermission()
        {
            int sdkInt = (int)Build.VERSION.SdkInt;

            if (sdkInt >= 30)
            {
                // Android 11+ (API 30+)
                LaunchFolderPicker();
            }
            else
            {
                //Xamarin.Essentials.Permissions.RequestAsync<Permissions.StorageWrite>();
                //Xamarin.Essentials.Permissions.RequestAsync<Permissions.StorageRead>();

                if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.WriteExternalStorage) != Permission.Granted)
                {
                    ActivityCompat.RequestPermissions(this,
                        new string[]
                        {
                            Manifest.Permission.WriteExternalStorage,
                            Manifest.Permission.ReadExternalStorage
                        },RequestStorageId);
                }
                else
                {
                    MessagingCenter.Send(String.Empty, BaseViewModel.MSG_SDCardPermissionsGranted);
                }
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

        private void RestorePersistedPermission()
        {
            try
            {
                if (_cfg.SDCardPathUri == null)
                    return;

                var uri = Android.Net.Uri.Parse(_cfg.SDCardPathUri);

                ContentResolver.TakePersistableUriPermission(
                    uri,
                    ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission
                );
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "RestorePersistedPermission failed");
            }
        }

        private void SubscribeMessages()
        {
            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_RequestSDCardPermissions, (message) =>
            {
                RequestStoragePermission();
            });

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
                    StopRecordNotification();
                    StopPlayingNotification();
                    Android.OS.Process.KillProcess(Android.OS.Process.MyPid());
                });
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_DisableDispatchKeyEvent, (message) =>
            {
                _dispatchKeyEventEnabled = false;
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_EnableDispatchKeyEvent, (message) =>
            {
                _dispatchKeyEventEnabledAt = DateTime.Now;
                _dispatchKeyEventEnabled = true;
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_RemoteKeyAction, (code) =>
            {
                SendRemoteKey(code);
            });
        }

        protected override void OnDestroy()
        {
            _loggingService.Info("Activity destroyed");

            _app.OnDestroy();

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

        private void SendRemoteKey(string code)
        {
            _loggingService.Debug($"SendRemoteKey: {code}");

            Android.Views.Keycode keyCode;
            if (Enum.TryParse<Android.Views.Keycode>(code, out keyCode))
            {
                new Instrumentation().SendKeyDownUpSync(keyCode);
            }
            else
            {
                _loggingService.Info("SendRemoteKey: invalid key code");
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            PermissionsImplementation.Current.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            if (requestCode == RequestStorageId)
            {
                if (grantResults.Length > 0 && grantResults[0] == Permission.Granted)
                {
                    MessagingCenter.Send(String.Empty, BaseViewModel.MSG_SDCardPermissionsGranted);
                }
                else
                {
                    ShowToastMessage("Oprávnění pro zápis na SD kartu bylo odmítnuto.");
                }
            }
        }


        private string GetFullPathFromTreeUri(Android.Net.Uri treeUri)
        {
            var docId = DocumentsContract.GetTreeDocumentId(treeUri);
            // např. "primary:Download" nebo "XXXX-XXXX:MyFolder"

            string[] split = docId.Split(':');
            if (split.Length < 2)
                return null;

            string type = split[0];
            string relativePath = split[1];

            if (type.Equals("primary", StringComparison.OrdinalIgnoreCase))
            {
                return $"{Android.OS.Environment.ExternalStorageDirectory}/{relativePath}";
            }
            else
            {
                // pravděpodobný mount point pro SD kartu
                return $"/storage/{type}/{relativePath}";
            }
        }


        protected override void OnActivityResult(int requestCode, Result resultCode, Android.Content.Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if (requestCode == RequestFolderAccessId && resultCode == Result.Ok && data != null)
            {
                Android.Net.Uri treeUri = data.Data;

                _loggingService.Info($"Setting External directory: {treeUri.ToString()}");

                var takeFlags = data.Flags & (ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission);
                ContentResolver.TakePersistableUriPermission(treeUri, takeFlags);

                _cfg.SDCardPathUri = treeUri.ToString();

                var path = GetFullPathFromTreeUri(treeUri);

                _loggingService.Info($"full path directory: {path}");

                MessagingCenter.Send(path, BaseViewModel.MSG_SDCardPermissionsGranted);
            }
        }

        public override bool OnKeyDown(Android.Views.Keycode keyCode, KeyEvent e)
        {
            return base.OnKeyDown(keyCode, e);
        }

        public override bool DispatchGenericMotionEvent(MotionEvent ev)
        {
            if (ev.Action == MotionEventActions.PointerIndexShift)
            {
                _loggingService.Info($"Wheel action axis: {ev.GetAxisValue(Axis.Vscroll)}");  // https://developer.android.com/reference/android/view/MotionEvent

                if (ev.GetAxisValue(Axis.Vscroll) <= 0)
                {
                    MessagingCenter.Send("down", BaseViewModel.MSG_KeyAction);
                }
                else
                {
                    MessagingCenter.Send("up", BaseViewModel.MSG_KeyAction);
                }

                return true; // disable further mouse wheel event propagation
            }
            else
            if (ev.Action == MotionEventActions.HoverMove)
            {
                return base.DispatchGenericMotionEvent(ev);
            } else
            {
                _loggingService.Info($"DispatchGenericMotionEvent: {ev.Action}");

                return base.DispatchGenericMotionEvent(ev);
            }
        }

        public override bool DispatchKeyEvent(KeyEvent e)
        {
            var code = e.KeyCode.ToString();

            if (e.Action == KeyEventActions.Up)
            {
                _loggingService.Debug($"DispatchKeyEvent: {code} consumed (ignoring key up event)");
                return true;
            }

            if (e.IsNumLockOn &&
                code != null &&
                code.ToLower().StartsWith("numpad") &&
                code.Length>6)
            {
                code = code.Substring(6);
            }

            var keyAction = KeyboardDeterminer.GetKeyAction(code);
            if (e.IsLongPress)
            {
                code = $"{BaseViewModel.LongPressPrefix}{code}";
            }

            if (_dispatchKeyEventEnabled)
            {
                // ignoring ENTER 1 second after DispatchKeyEvent enabled

                var ms = (DateTime.Now - _dispatchKeyEventEnabledAt).TotalMilliseconds;

                if (keyAction == KeyboardNavigationActionEnum.OK && ms < 1000)
                {
                    _loggingService.Debug($"DispatchKeyEvent: {code} -> ignoring OK action");

                    return true;
                }
                else
                {
                    _loggingService.Debug($"DispatchKeyEvent: {code} -> sending to ancestor");
                    return base.DispatchKeyEvent(e);
                }
            }
            else
            {
                if (keyAction != KeyboardNavigationActionEnum.Unknown)
                {
                    _loggingService.Debug($"DispatchKeyEvent: {code} -> sending to application, time: {e.EventTime - e.DownTime}");

                    MessagingCenter.Send(code, BaseViewModel.MSG_KeyAction);

                    return true;
                }
                else
                {
                    // unknown key

                    _loggingService.Debug($"DispatchKeyEvent: {code} -> unknown key sending to ancestor");
#if DEBUG
                    ShowToastMessage($"<{code}>");
#endif
                    return base.DispatchKeyEvent(e);
                }
            }
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
                        MessagingCenter.Send("Right", BaseViewModel.MSG_KeyAction);
                        return true;
                    }

                    if (x < -0.5 || x1 < -0.5)
                    {
                        MessagingCenter.Send("Left", BaseViewModel.MSG_KeyAction);
                        return true;
                    }

                    if (y > 0.5 || y1 > 0.5)
                    {
                        MessagingCenter.Send("Down", BaseViewModel.MSG_KeyAction);
                        return true;
                    }

                    if (y < -0.5 || y1 < -0.5)
                    {
                        MessagingCenter.Send("Up", BaseViewModel.MSG_KeyAction);
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