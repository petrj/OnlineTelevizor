﻿using LoggerService;
using OnlineTelevizor.Models;
using OnlineTelevizor.Services;
using Plugin.Permissions;
using Plugin.Permissions.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace OnlineTelevizor.ViewModels
{
    public class BaseViewModel : BaseNotifableObject
    {
        protected ILoggingService _loggingService;
        protected IDialogService _dialogService;

        public const string LongPressPrefix = "LongPress:";

        public const string MSG_UriMessage = "LaunchUriMessage";
        public const string MSG_EnableFullScreen = "EnableFullScreen";
        public const string MSG_DisableFullScreen = "DisableFullScreen";
        public const string MSG_PlayInternal = "PlayInternal";
        public const string MSG_PlayInternalNotification = "PlayInternalNotification";
        public const string MSG_UpdateInternalNotification = "UpdateInternalNotification";
        public const string MSG_StopPlayInternalNotification = "StopPlayInternalNotification";
        public const string MSG_StopPlayInternalNotificationAndQuit = "StopPlayInternalNotificationAndQuit";
        public const string MSG_KeyAction = "KeyAction";
        public const string MSG_RemoteKeyAction = "RemoteKeyAction";
        public const string MSG_ShowDetailMessage = "ShowDetailMessage";
        public const string MSG_ShowRenderers = "ShowRenderers";
        public const string MSG_ShowTimer = "ShowTimer";
        public const string MSG_CastingStarted = "CastingStarted";
        public const string MSG_CastingStopped = "CastingStopped";
        public const string MSG_StopCasting = "StopCasting";
        public const string MSG_StopPlay = "StopPlay";
        public const string MSG_PlayInPreview = "PlayInPreview";
        public const string MSG_ToastMessage = "ShowToastMessage";
        public const string MSG_ShareUrl = "Sdílet odkaz";
        public const string MSG_ShowConfiguration = "ShowConfiguration";
        public const string MSG_StartCastNotification = "StartCastNotification";
        public const string MSG_StopCastNotification = "StopCastNotification";
        public const string MSG_RecordNotificationMessage = "RecordNotification";
        public const string MSG_UpdateRecordNotificationMessage = "UpdateRecordNotification";
        public const string MSG_StopRecordNotificationMessage = "StopRecordNotification";
        public const string MSG_StopRecord = "StopRecord";
        public const string MSG_ToggleAudioStream = "ToggleAudioStream";
        public const string MSG_ToggleAudioStreamId = "ToggleAudioStreamId";
        public const string MSG_ToggleSubtitles = "ToggleSubtitles";
        public const string MSG_CheckBatterySettings = "CheckBatterySettings";
        public const string MSG_RequestBatterySettings = "RequestBatterySettings";
        public const string MSG_SetBatterySettings = "SetBatterySettings ";
        public const string MSG_DisableDispatchKeyEvent = "DisableDispatchKeyEvent";
        public const string MSG_EnableDispatchKeyEvent = "EnableDispatchKeyEvent";
        public const string MSG_RequestSDCardPermissions = "RequestSDCardPermissions";
        public const string MSG_SDCardPermissionsGranted = "SDCardPermissionsGranted";

        public IOnlineTelevizorConfiguration Config { get; set; }

        bool isBusy = false;

        private ChannelItem _playingChannel = null;

        public BaseViewModel(ILoggingService loggingService, IOnlineTelevizorConfiguration config, IDialogService dialogService)
        {
            _loggingService = loggingService;
            _dialogService = dialogService;
            Config = config;
        }

        public static string DeviceFriendlyName
        {
            get
            {
                return $"{Xamarin.Essentials.DeviceInfo.Manufacturer} {Xamarin.Essentials.DeviceInfo.Model}";
            }
        }

        public static int GetScaledSize(IOnlineTelevizorConfiguration Config, int normalSize)
        {
            switch (Config.AppFontSize)
            {
                case AppFontSizeEnum.AboveNormal:
                    return Convert.ToInt32(Math.Round(normalSize * 1.12));
                case AppFontSizeEnum.Big:
                    return Convert.ToInt32(Math.Round(normalSize * 1.25));
                case AppFontSizeEnum.Bigger:
                    return Convert.ToInt32(Math.Round(normalSize * 1.5));
                case AppFontSizeEnum.VeryBig:
                    return Convert.ToInt32(Math.Round(normalSize * 1.75));
                case AppFontSizeEnum.Huge:
                    return Convert.ToInt32(Math.Round(normalSize * 2.0));
                default: return normalSize;
            }
        }

        public int GetScaledSize(int normalSize)
        {
            return GetScaledSize(Config, normalSize);
        }

        public bool IsBusy
        {
            get { return isBusy; }
            set
            {
                SetProperty(ref isBusy, value);
            }
        }

        string title = string.Empty;
        public string Title
        {
            get { return title; }
            set { SetProperty(ref title, value); }
        }

        public ChannelItem PlayingChannel
        {
            get { return _playingChannel; }
            set
            {
                _playingChannel = value;

                OnPropertyChanged(nameof(PlayingChannelLogoIcon));
            }
        }

        public string PlayingChannelLogoIcon
        {
            get
            {
                if (PlayingChannel == null)
                    return String.Empty;

                return PlayingChannel.LogoUrl;
            }
        }

        #region Permissions

        protected async Task RequestPermission(Permission perm)
        {
            void emptyAction() { }
            await RunWithPermission(perm, new Command(emptyAction));
        }

        protected async Task RunWithPermission(Permission perm, Command command)
        {
            await RunWithPermission(perm, new List<Command>() { command });
        }

        protected async Task RunWithPermission(Permission perm, List<Command> commands)
        {
            var f = new Func<Task>(
                 async () =>
                 {
                     foreach (var command in commands)
                     {
                         await Task.Run(() => command.Execute(null));
                     }
                 });

            await RunWithPermission(perm, f);
        }

        protected async Task RunWithPermission(Permission perm, Func<Task> action)
        {
            try
            {
                var status = await CrossPermissions.Current.CheckPermissionStatusAsync(perm);
                if (status != PermissionStatus.Granted)
                {
                    if (await CrossPermissions.Current.ShouldShowRequestPermissionRationaleAsync(perm))
                    {
                        await _dialogService.Information("Aplikace vyžaduje potvrzení k oprávnění.");
                    }

                    var results = await CrossPermissions.Current.RequestPermissionsAsync(perm);

                    if (results.ContainsKey(perm))
                        status = results[perm];
                }

                if (status == PermissionStatus.Granted)
                {
                    await action();
                }
                else if (status != PermissionStatus.Unknown)
                {
                    await _dialogService.Information("Oprávnění nebylo uděleno");
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
            }
        }

        #endregion

        public async Task Play(ChannelItem channel)
        {
            try
            {
                if (Config.InternalPlayer)
                {
                    MessagingCenter.Send<BaseViewModel, ChannelItem> (this, BaseViewModel.MSG_PlayInternal, channel);
                }
                else
                {
                    MessagingCenter.Send(channel.Url, BaseViewModel.MSG_UriMessage);
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "PlayStream general error");

                await _dialogService.Information(ex.ToString());
            }
        }

        public bool EmptyCredentials
        {
            get
            {
                if (Config.TVApi == TVAPIEnum.Demo)
                    return false;

                if (Config.TVApi == TVAPIEnum.SledovaniTV &&
                    String.IsNullOrEmpty(Config.Username) &&
                    String.IsNullOrEmpty(Config.Password))
                    return true;

                if (Config.TVApi == TVAPIEnum.KUKI &&
                    String.IsNullOrEmpty(Config.KUKIsn))
                    return true;

                if (Config.TVApi == TVAPIEnum.O2TV &&
                    String.IsNullOrEmpty(Config.O2TVUsername) &&
                    String.IsNullOrEmpty(Config.O2TVPassword))
                    return true;

                return false;
            }
        }
    }
}
