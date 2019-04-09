using Android.App;
using Android.Content;
using LoggerService;
using SledovaniTVAPI;
using SledovaniTVPlayer.Models;
using SledovaniTVPlayer.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Plugin.Permissions;
using Plugin.Permissions.Abstractions;


namespace SledovaniTVPlayer.ViewModels
{
    public class MainPageViewModel : BaseViewModel
    {
        private TVService _service;
        private ISledovaniTVConfiguration _config;

        public ObservableCollection<TVChannel> Channels { get; set; }

        public string StatusLabel
        {
            get
            {
                switch (_service.Status)
                {
                    case StatusEnum.NotInitialized: return "Probíhá načítání kanálů ...";
                    case StatusEnum.EmptyCredentials: return "Nevyplněny přihlašovací údaje";
                    case StatusEnum.Logged: return $"Načteno {Channels.Count} kanálů";
                    case StatusEnum.LoginFailed: return $"Chybné přihlašovací údaje";
                    case StatusEnum.Paired: return $"Probíhá přihlašování ...";
                    case StatusEnum.PairingFailed: return $"Chybné přihlašovací údaje";
                    default: return String.Empty;
                }
            }
        }

        public Command RefreshCommand { get; set; }
        public Command ResetConnectionCommand { get; set; }

        public Command RequestWriteLogsPermissionsCommand { get; set; }

        public MainPageViewModel(ILoggingService loggingService, ISledovaniTVConfiguration config, IDialogService dialogService, Context context)
           : base(loggingService, config, dialogService, context)
        {
            _service = new TVService(loggingService, config);
            _loggingService = loggingService;
            _dialogService = dialogService;
            _context = context;
            _config = config;

            Channels = new ObservableCollection<TVChannel>();

            RefreshCommand = new Command(async () => await ReloadChannels());
            ResetConnectionCommand = new Command(async () => await ResetConnection());

            RequestWriteLogsPermissionsCommand = new Command(async () => await RequestWriteLogsPermissions());

            RequestWriteLogsPermissionsCommand.Execute(null);

            // refreshing every 30 s
            //BackgroundCommandWorker.RunInBackground(RefreshCommand, 30, 0);

            RefreshCommand.Execute(null);
        }

        private async Task RequestWriteLogsPermissions()
        {
            if (!_config.EnableLogging)
              return;

            await RequestPermission(Permission.Storage);
        }

        private async Task ReloadChannels()
        {
            IsBusy = true;

            try
            {
                Channels.Clear();

                var channels = await _service.GetChannels();

                foreach (var ch in channels)
                    Channels.Add(ch);

            } finally
            {
                IsBusy = false;
                OnPropertyChanged(nameof(StatusLabel));
                OnPropertyChanged(nameof(IsBusy));
            }
        }

        private async Task ResetConnection()
        {
            await _service.ResetConnection();
            OnPropertyChanged(nameof(StatusLabel));
        }
    }
}
