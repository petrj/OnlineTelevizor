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
using Android.Content;
using System.Text.RegularExpressions;
using Plugin.InAppBilling;
using Plugin.InAppBilling.Abstractions;

namespace OnlineTelevizor.ViewModels
{
    public class BaseViewModel : BaseNotifableObject
    {
        protected ILoggingService _loggingService;
        protected IDialogService _dialogService;
        public const string UriMessage = "LaunchUriMessage";
        public const string KeyMessage = "KeyDownMessage";
        public const string ShowDetailMessage = "ShowDetailMessage";
        public const string ToastMessage = "ShowToastMessage";

        public IOnlineTelevizorConfiguration Config { get; set; }

        bool isBusy = false;

        public BaseViewModel(ILoggingService loggingService, IOnlineTelevizorConfiguration config, IDialogService dialogService)
        {
            _loggingService = loggingService;
            _dialogService = dialogService;
            Config = config;
        }
        
        public int GetScaledSize(int normalSize)
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

        public async Task PlayStream(string url, int resultKeyCode = 0)
        {
            try
            {
                // apply config quality:
                if (!String.IsNullOrEmpty(Config.StreamQuality))
                {
                    var configQuality = "quality=" + Config.StreamQuality;

                    var qMatches = Regex.Match(url, "quality=[0-9]{1,4}");
                    if (qMatches != null && qMatches.Success)
                    {
                        url = url.Replace(qMatches.Value, configQuality);
                    }
                    else
                    {
                        url += "&" + configQuality;
                    }
                }

                if (Device.RuntimePlatform == Device.UWP)
                {
                    MessagingCenter.Send(url, BaseViewModel.UriMessage);
                }
                else
                if (Device.RuntimePlatform == Device.Android)
                {  
                    var intent = new Intent(Intent.ActionView);
                    var uri = Android.Net.Uri.Parse(url);
                    intent.SetDataAndType(uri, "video/*");
                    intent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTask); // necessary for Android 5
                    Android.App.Application.Context.StartActivity(intent);
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "PlayStream general error");

                await _dialogService.Information(ex.ToString());
            }
        }
    }
}
