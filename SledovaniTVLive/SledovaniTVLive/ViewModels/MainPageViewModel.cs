using Android.App;
using Android.Content;
using LoggerService;
using SledovaniTVAPI;
using SledovaniTVLive.Models;
using SledovaniTVLive.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Plugin.Permissions;
using Plugin.Permissions.Abstractions;


namespace SledovaniTVLive.ViewModels
{
    public class MainPageViewModel : BaseViewModel
    {
        private TVService _service;
        private ISledovaniTVConfiguration _config;

        public ObservableCollection<ChannelItem> Channels { get; set; } = new ObservableCollection<ChannelItem>();

        private Dictionary<string, ChannelItem> _channelById { get; set; } = new Dictionary<string, ChannelItem>();

        public string ChannelNameFilter { get; set; }

        public TVService TVService
        {
            get
            {
                return _service;
            }
        }

        public string StatusLabel
        {
            get
            {
                if (IsBusy)
                {
                    return "Aktualizují se kanály...";
                }

                switch (_service.Status)
                {
                    case StatusEnum.NotInitialized: return "";
                    case StatusEnum.EmptyCredentials: return "Nevyplněny přihlašovací údaje";
                    case StatusEnum.Logged: return $"Načteno {Channels.Count} kanálů";
                    case StatusEnum.LoginFailed: return $"Chybné přihlašovací údaje";
                    case StatusEnum.Paired: return $"Uživatel přihlášen";
                    case StatusEnum.PairingFailed: return $"Chybné přihlašovací údaje";
                    default: return String.Empty;
                }
            }
        }

        public Command RefreshCommand { get; set; }

        public Command RefreshChannlesCommand { get; set; }
        public Command RefreshEPGCommand { get; set; }

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

            RefreshCommand = new Command(async () => await Refresh());

            RefreshEPGCommand = new Command(async () => await RefreshEPG());
            RefreshChannlesCommand = new Command(async () => await RefreshChannels());

            ResetConnectionCommand = new Command(async () => await ResetConnection());

            RequestWriteLogsPermissionsCommand = new Command(async () => await RequestWriteLogsPermissions());

            RequestWriteLogsPermissionsCommand.Execute(null);

            RefreshChannlesCommand.Execute(null);

            // refreshing every min with 2 s start delay
            BackgroundCommandWorker.RunInBackground(RefreshEPGCommand, 60, 2);
        }

        public Dictionary<string, ChannelItem> ChannelById
        {
            get
            {
                return _channelById;
            }
        }

        private async Task RequestWriteLogsPermissions()
        {
            if (!_config.EnableLogging)
              return;

            await RequestPermission(Permission.Storage);
        }

        private async Task Refresh()
        {
            await RefreshChannels(false);
            await RefreshEPG();
        }

        private async Task RefreshEPG(bool SetFinallyNotBusy = true)
        {
            IsBusy = true;

            try
            {
                OnPropertyChanged(nameof(StatusLabel));

                foreach (var channelItem in Channels)
                {
                    channelItem.ClearEPG();
                }

                var epg = await _service.GetEPG();
                foreach (var ei in epg)
                {
                    if (ChannelById.ContainsKey(ei.ChannelId))
                    {
                        // updating channel EPG

                        var ch = ChannelById[ei.ChannelId];
                        ch.AddEPGItem(ei);
                    }
                }
            }
            finally
            {
                if (SetFinallyNotBusy)
                {
                    IsBusy = false;
                }

                OnPropertyChanged(nameof(StatusLabel));
                OnPropertyChanged(nameof(IsBusy));
            }
        }

        private async Task RefreshChannels(bool SetFinallyNotBusy = true)
        {
            IsBusy = true;

            try
            {
                OnPropertyChanged(nameof(StatusLabel));

                Channels.Clear();
                _channelById.Clear();

                var channels = await _service.GetChannels();

                foreach (var ch in channels)
                {
                    if (_config.ChannelGroup != "*" &&
                        _config.ChannelGroup != null &&
                        _config.ChannelGroup != ch.Group)
                        continue;

                    if (_config.ChannelType != "*" &&
                        _config.ChannelType != null &&
                        _config.ChannelType != ch.Type)
                        continue;

                    if ((!String.IsNullOrEmpty(ChannelNameFilter)) &&
                        (ChannelNameFilter != "*") &&
                        !ch.Name.ToLower().Contains(ChannelNameFilter.ToLower()))
                        continue;

                    Channels.Add(ch);
                    _channelById.Add(ch.Id, ch); // for faster EPG refreesh
                }

            } finally
            {
                if (SetFinallyNotBusy)
                {
                    IsBusy = false;
                }
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
