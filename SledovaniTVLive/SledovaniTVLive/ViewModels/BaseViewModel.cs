using LoggerService;
using SledovaniTVLive.Models;
using SledovaniTVLive.Services;
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

namespace SledovaniTVLive.ViewModels
{
    public class BaseViewModel : BaseNotifableObject
    {
        protected ILoggingService _loggingService;
        protected IDialogService _dialogService;
        protected Context _context;

        public ISledovaniTVConfiguration Config { get; set; }

        // navigation.PushModalAsync enabled only 1 time per 3 seconds
        private DateTime _lastNavigateTime = DateTime.MinValue;

        bool isBusy = false;

        public BaseViewModel(ILoggingService loggingService, ISledovaniTVConfiguration config, IDialogService dialogService, Context context)
        {
            _loggingService = loggingService;
            _dialogService = dialogService;
            _context = context;
            Config = config;
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
        
        public async Task NavigateToPage(Page page, INavigation navigation)
        {
            if ((DateTime.Now - _lastNavigateTime).TotalSeconds > 3)
            {
                _lastNavigateTime = DateTime.Now;

                var navPage = new NavigationPage(page);
                //NavigationPage.SetHasBackButton(page, true);
                await navigation.PushAsync(page);
            }
        }

        protected async Task ShareFile(string fileName)
        {
            try
            {
                var intent = new Intent(Intent.ActionSend);
                var file = new Java.IO.File(fileName);
                var uri = Android.Net.Uri.FromFile(file);
                intent.PutExtra(Intent.ExtraStream, uri);
                intent.SetDataAndType(uri, "text/plain");
                intent.SetFlags(ActivityFlags.GrantReadUriPermission);
                intent.SetFlags(ActivityFlags.NewTask);
                _context.StartActivity(intent);
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
                await _dialogService.Information($"Při sdílení logu došlo k chybě: {ex.Message}");
            }
        }

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
                    } else
                    {
                        url += "&" + configQuality;
                    }
                }                

                var intent = new Intent(Intent.ActionView);
                var uri = Android.Net.Uri.Parse(url);
                intent.SetDataAndType(uri, "video/*");
                intent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTask); // necessary for Android 5
                _context.StartActivity(intent);
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "PlayStream general error");

                await _dialogService.Information(ex.ToString());
            }
        }
    }
}
