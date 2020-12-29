using LoggerService;
using OnlineTelevizor.Models;
using OnlineTelevizor.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xamarin.Forms;
using Plugin.Permissions;
using Plugin.Permissions.Abstractions;
using System.Text.RegularExpressions;


namespace OnlineTelevizor.ViewModels
{
    public class BaseViewModel : BaseNotifableObject
    {
        protected ILoggingService _loggingService;
        protected IDialogService _dialogService;
        public const string UriMessage = "LaunchUriMessage";
        public const string EnableFullScreen = "EnableFullScreen";
        public const string DisableFullScreen = "DisableFullScreen";
        public const string PlayInternal = "PlayInternal";
        public const string KeyMessage = "KeyDownMessage";
        public const string ShowDetailMessage = "ShowDetailMessage";
        public const string ToastMessage = "ShowToastMessage";
        public const string ShowConfiguration = "ShowConfiguration";
        
        public const string CheckBatterySettings = "CheckBatterySettings";
        public const string RequestBatterySettings = "RequestBatterySettings";
        public const string SetBatterySettings = "SetBatterySettings ";

        public IOnlineTelevizorConfiguration Config { get; set; }

        bool isBusy = false;

        public BaseViewModel(ILoggingService loggingService, IOnlineTelevizorConfiguration config, IDialogService dialogService)
        {
            _loggingService = loggingService;
            _dialogService = dialogService;
            Config = config;
        }

        public static int GetScaledSize(IOnlineTelevizorConfiguration Config, int normalSize)
        {
            switch (Config.AppFontSize)
            {
                case AppFontSizeEnum.AboveNormal:
                    return Convert.ToInt32(Math.Round(normalSize * 1.12));
                case AppFontSizeEnum.Big:
                    return Convert.ToInt32(Math.Round(normalSize * 1.25));
                case AppFontSizeEnum.Biger:
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

        public async Task PlayStream(MediaDetail mediaDetail)
        {
            try
            {
                // apply config quality:
                if (!String.IsNullOrEmpty(Config.StreamQuality))
                {
                    var configQuality = "quality=" + Config.StreamQuality;

                    var qMatches = Regex.Match(mediaDetail.MediaUrl, "quality=[0-9]{1,4}");
                    if (qMatches != null && qMatches.Success)
                    {
                        mediaDetail.MediaUrl = mediaDetail.MediaUrl.Replace(qMatches.Value, configQuality);
                    }
                    else
                    {
                        mediaDetail.MediaUrl += "&" + configQuality;
                    }
                }

                if (Config.InternalPlayer)
                {
                    MessagingCenter.Send<BaseViewModel, MediaDetail> (this, BaseViewModel.PlayInternal, mediaDetail);
                }
                else
                {
                    MessagingCenter.Send(mediaDetail.MediaUrl, BaseViewModel.UriMessage);
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
                if (Config.TVApi == TVAPIEnum.SledovaniTV &&
                    String.IsNullOrEmpty(Config.Username) &&
                    String.IsNullOrEmpty(Config.Password))
                    return true;

                if (Config.TVApi == TVAPIEnum.KUKI &&
                    String.IsNullOrEmpty(Config.KUKIsn))
                    return true;

                if (Config.TVApi == TVAPIEnum.DVBStreamer &&
                    String.IsNullOrEmpty(Config.DVBStreamerUrl))
                    return true;

                return false;
            }
        }
    }
}
