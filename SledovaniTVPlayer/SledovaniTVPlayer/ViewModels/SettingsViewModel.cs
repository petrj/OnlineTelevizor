using Android.Content;
using LoggerService;
using SledovaniTVPlayer.Models;
using SledovaniTVPlayer.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Plugin.Permissions;
using Plugin.Permissions.Abstractions;

namespace SledovaniTVPlayer.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        private ILoggingService _loggingService;
        private Context _context;
        private IDialogService _dialogService;
        private ISledovaniTVConfiguration _config;

        public Command ShareLogCommand { get; set; }

        public SettingsViewModel(ILoggingService loggingService, ISledovaniTVConfiguration config, Context context, IDialogService dialogService)
            : base(loggingService, config, dialogService, context)
        {
            _loggingService = loggingService;
            _context = context;
            _dialogService = dialogService;
            _config = config;

            ShareLogCommand = new Command(async () => await ShareLogWithPermissionsRequest());
        }

        public int LoggingLevelIndex
        {
            get
            {
                return (int)_config.LoggingLevel;
            }
            set
            {
                _config.LoggingLevel = (LoggingLevelEnum)value;
                OnPropertyChanged(nameof(LoggingLevelIndex));
            }
        }

        private async Task ShareLogWithPermissionsRequest()
        {
            await RunWithPermission(Permission.Storage, async () => await ShareLog());
        }

        private async Task ShareLog()
        {
            if (!_config.EnableLogging)
            {
                await _dialogService.Information("Logování není povoleno");
                return;
            }

            if (!(_loggingService is BasicLoggingService))
            {
                await _dialogService.Information("Logování bude probíhat až po restartování aplikace");
                return;
            }

            var fName = (_loggingService as BasicLoggingService).LogFilename;

            if (!File.Exists(fName))
            {
                await _dialogService.Information($"Log {fName} nebyl nalezen");
                return;
            }

            await ShareFile(fName);
        }
    }
}
