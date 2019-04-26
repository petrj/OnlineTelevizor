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
using System.Threading;
using Plugin.InAppBilling;
using Plugin.InAppBilling.Abstractions;

namespace SledovaniTVLive.ViewModels
{
    public class MainPageViewModel : BaseViewModel
    {
        private TVService _service;
        private ISledovaniTVConfiguration _config;
        private static SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

        public ObservableCollection<ChannelItem> Channels { get; set; } = new ObservableCollection<ChannelItem>();

        private Dictionary<string, ChannelItem> _channelById { get; set; } = new Dictionary<string, ChannelItem>();

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
                    case StatusEnum.GeneralError: return $"Chyba";
                    case StatusEnum.ConnectionNotAvailable: return $"Chyba připojení";
                    case StatusEnum.NotInitialized: return "";
                    case StatusEnum.EmptyCredentials: return "Nevyplněny přihlašovací údaje";
                    case StatusEnum.Logged: return GetChannelsStatus;
                    case StatusEnum.LoginFailed: return $"Chybné přihlašovací údaje";
                    case StatusEnum.Paired: return $"Uživatel přihlášen";
                    case StatusEnum.PairingFailed: return $"Chybné přihlašovací údaje";
                    case StatusEnum.BadPin: return $"Chybný PIN";
                    default: return String.Empty;
                }
            }
        }

        private string GetChannelsStatus
        {
            get
            {
                string status = String.Empty;

                if (!_config.Purchased)
                    status = "Verze zdarma. ";

                if (Channels.Count == 0)
                {
                    return $"{status}Není k dispozici žádný kanál";
                } else
                if (Channels.Count == 1)
                {
                    return $"{status}Načten 1 kanál";
                } else
                if ((Channels.Count >= 2) && (Channels.Count <= 4))
                {
                    return $"{status}Načteny {Channels.Count} kanály";
                } else
                {
                    return $"{status}Načteno {Channels.Count} kanálů";
                }
            }
        }

        public Command RefreshCommand { get; set; }

        public Command RefreshChannelsCommand { get; set; }
        public Command RefreshEPGCommand { get; set; }
        public Command ResetConnectionCommand { get; set; }
        public Command CheckPurchaseCommand { get; set; }

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

            CheckPurchaseCommand = new Command(async () => await CheckPurchase());

            RefreshEPGCommand = new Command(async () => await RefreshEPG());
            RefreshChannelsCommand = new Command(async () => await RefreshChannels());

            ResetConnectionCommand = new Command(async () => await ResetConnection());

            RequestWriteLogsPermissionsCommand = new Command(async () => await RequestWriteLogsPermissions());
            
            RequestWriteLogsPermissionsCommand.Execute(null);

            RefreshChannelsCommand.Execute(null);

            // refreshing every min with 3s start delay
            BackgroundCommandWorker.RunInBackground(RefreshEPGCommand, 60, 3);
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
            await _semaphoreSlim.WaitAsync();

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

                _semaphoreSlim.Release();
            }      
        }

        private async Task RefreshChannels(bool SetFinallyNotBusy = true)
        {
            await _semaphoreSlim.WaitAsync();

            await CheckPurchase();

            IsBusy = true;

            try
            {
                OnPropertyChanged(nameof(StatusLabel));

                Channels.Clear();
                _channelById.Clear();

                var channels = await _service.GetChannels();

                foreach (var ch in channels)
                {
                    if (_config.ChannelFilterGroup != "*" &&
                        _config.ChannelFilterGroup != null &&
                        _config.ChannelFilterGroup != ch.Group)
                        continue;

                    if (_config.ChannelFilterType != "*" &&
                        _config.ChannelFilterType != null &&
                        _config.ChannelFilterType != ch.Type)
                        continue;

                    if ((!String.IsNullOrEmpty(_config.ChannelFilterName)) &&
                        (_config.ChannelFilterName != "*") &&                        
                        !ch.Name.ToLower().Contains(_config.ChannelFilterName.ToLower()))
                        continue;

                    Channels.Add(ch);
                    _channelById.Add(ch.Id, ch); // for faster EPG refresh
                }

            } finally
            {
                if (SetFinallyNotBusy)
                {
                    IsBusy = false;
                }
                OnPropertyChanged(nameof(StatusLabel));
                OnPropertyChanged(nameof(IsBusy));

                _semaphoreSlim.Release();
            }
        }

        private async Task ResetConnection()
        {
            await _service.ResetConnection();
            OnPropertyChanged(nameof(StatusLabel));
        }

        public async Task CheckPurchase()
        {
            if (_config.Purchased)
                return;

            _loggingService.Debug($"Checking purchase");

            try
            {
                if (!String.IsNullOrEmpty(_config.PurchaseId))
                {
                    // purchase information found in config
                    _config.Purchased = true;
                    _loggingService.Debug($"Already purchased (purchase id in config)");
                    return;
                }

                // contacting service

                var connected = await CrossInAppBilling.Current.ConnectAsync();

                if (!connected)
                {
                    _loggingService.Info($"Connection to AppBilling service failed");
                    await _dialogService.Information("Nepodařilo se ověřit stav zaplacení plné verze.");
                    return;
                }

                var purchases = await CrossInAppBilling.Current.GetPurchasesAsync(ItemType.InAppPurchase);
                foreach (var purchase in purchases)
                {
                    if (purchase.ProductId == _config.PurchaseProductId)
                    {
                        _config.Purchased = true;
                        _config.PurchaseId = purchase.Id;
                        _config.PurchaseToken = purchase.PurchaseToken;

                        _loggingService.Debug($"Already purchased");
                        break;
                    }
                }

            } catch (Exception ex)
            {
                _loggingService.Error(ex, "Error while checking purchase");            
            }
            finally
            {
                await CrossInAppBilling.Current.DisconnectAsync();
            }
        }
    }
}
