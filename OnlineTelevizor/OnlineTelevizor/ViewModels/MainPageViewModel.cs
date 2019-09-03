using LoggerService;
using SledovaniTVAPI;
using OnlineTelevizor.Models;
using OnlineTelevizor.Services;
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
using TVAPI;

namespace OnlineTelevizor.ViewModels
{
    public class MainPageViewModel : BaseViewModel
    {
        private TVService _service;
        private static SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

        public ObservableCollection<ChannelItem> Channels { get; set; } = new ObservableCollection<ChannelItem>();
        public ObservableCollection<ChannelItem> AllNotFilteredChannels { get; set; } = new ObservableCollection<ChannelItem>();

        private Dictionary<string, ChannelItem> _channelById { get; set; } = new Dictionary<string, ChannelItem>();

        private ChannelItem _selectedItem;
        private bool _firstRefresh = true;
        private int _lastRefreshDelay = 0;

        public TVService TVService
        {
            get
            {
                return _service;
            }
        }

        public bool IsPortrait { get; set; } = true;
        public bool DoNotScrollToChannel { get; set; } = false;

        public Command RefreshCommand { get; set; }

        public Command PlayCommand { get; set; }

        public Command RefreshChannelsCommand { get; set; }
        public Command RefreshEPGCommand { get; set; }
        public Command ResetConnectionCommand { get; set; }
        public Command CheckPurchaseCommand { get; set; }

        public Command LongPressCommand { get; set; }
        public Command ShortPressCommand { get; set; }

        public MainPageViewModel(ILoggingService loggingService, IOnlineTelevizorConfiguration config, IDialogService dialogService)
           : base(loggingService, config, dialogService)
        {
            _loggingService.Info("Initializing MainPageViewModel");

            _service = new TVService(loggingService, config);
            _loggingService = loggingService;
            _dialogService = dialogService;
            Config = config;

            RefreshCommand = new Command(async () => await Refresh());

            CheckPurchaseCommand = new Command(async () => await CheckPurchase());

            RefreshEPGCommand = new Command(async () => await RefreshEPG());
            RefreshChannelsCommand = new Command(async () => await RefreshChannels());

            ResetConnectionCommand = new Command(async () => await ResetConnection());

            PlayCommand = new Command(async () => await Play());

            LongPressCommand = new Command(LongPress);
            ShortPressCommand = new Command(ShortPress);

            // refreshing channels every hour with no start delay
            BackgroundCommandWorker.RunInBackground(RefreshChannelsCommand, 3600, 0);

            // refreshing EPG every min with 3s start delay
            BackgroundCommandWorker.RunInBackground(RefreshEPGCommand, 60, 3);
        }

        private void LongPress(object item)
        {
            if (item != null && item is ChannelItem)
            {
                // select and show program epg detail

                SelectedItem = item as ChannelItem;

                _loggingService.Info($"Long press (channel {SelectedItem.Name})");

                DoNotScrollToChannel = true;

                if (IsPortrait)
                {
                    MessagingCenter.Send<MainPageViewModel>(this, BaseViewModel.ShowDetailMessage);
                }
            }
        }

        private void ShortPress(object item)
        {
            if (item != null && item is ChannelItem)
            {
                // select and play

                SelectedItem = item as ChannelItem;

                _loggingService.Info($"Short press (channel {SelectedItem.Name})");

                Task.Run(async () => await Play());
            }
        }

        public async Task SelectChannelByNumber(string number)
        {
            _loggingService.Info($"Selecting channel by number {number}");

            await _semaphoreSlim.WaitAsync();

            await Task.Run(
                () =>
                {
                    try
                    {
                        // looking for channel by its number:
                        foreach (var ch in Channels)
                        {
                            if (ch.ChannelNumber == number)
                            {
                                SelectedItem = ch;
                                break;
                            }
                        }
                    }
                    finally
                    {
                        _semaphoreSlim.Release();
                    };
                });
        }

        public string SelectedChannelEPGTitle
        {
            get
            {
                if (SelectedItem == null || SelectedItem.CurrentEPGItem == null)
                    return String.Empty;

                return SelectedItem.CurrentEPGItem.Title;
            }
        }

        public double SelectedChannelEPGProgress
        {
            get
            {
                if (SelectedItem == null || SelectedItem.CurrentEPGItem == null)
                    return 0;

                return SelectedItem.CurrentEPGItem.Progress;
            }
        }

        public Color EPGProgressBackgroundColor
        {
            get
            {
                if (SelectedItem == null || SelectedItem.CurrentEPGItem == null)
                    return Color.Black;

                return Color.White;
            }
        }

        public string SelectedChannelEPGDescription
        {
            get
            {
                if (SelectedItem == null || SelectedItem.CurrentEPGItem == null)
                    return String.Empty;

                return SelectedItem.CurrentEPGItem.Description;
            }
        }


        public ChannelItem SelectedItem
        {
            get
            {
                return _selectedItem;
            }
            set
            {
                _selectedItem = value;
                if (value != null)
                    Config.LastChannelNumber = value.ChannelNumber;

                OnPropertyChanged(nameof(SelectedItem));
                OnPropertyChanged(nameof(SelectedChannelEPGTitle));
                OnPropertyChanged(nameof(SelectedChannelEPGDescription));
                OnPropertyChanged(nameof(SelectedChannelEPGProgress));
                OnPropertyChanged(nameof(EPGProgressBackgroundColor));
            }
        }

        public async Task SelectNextChannel(int step = 1)
        {
            _loggingService.Info($"Selecting next channel (step {step})");

            await _semaphoreSlim.WaitAsync();

            await Task.Run(
                () =>
                {
                try
                {
                    if (Channels.Count == 0)
                        return;

                        if (SelectedItem == null)
                        {
                            SelectedItem = Channels[0];
                        }
                        else
                        {
                            bool next = false;
                            var nextCount = 1;
                            for (var i = 0; i < Channels.Count; i++)
                            {
                                var ch = Channels[i];

                                if (next)
                                {
                                    if (nextCount == step || i == Channels.Count-1)
                                    {
                                        SelectedItem = ch;
                                        break;
                                    }
                                    else
                                    {
                                        nextCount++;
                                    }
                                }
                                else
                                {
                                    if (ch == SelectedItem)
                                    {
                                        next = true;
                                    }
                                }
                            }
                        }
                    }
                    finally
                    {
                        _semaphoreSlim.Release();
                    };
                });
        }

        public async Task SelectPreviousChannel(int step = 1)
        {
            _loggingService.Info($"Selecting previous channel (step {step})");

            await _semaphoreSlim.WaitAsync();

            await Task.Run(
                () =>
                {
                    try
                    {
                        if (Channels.Count == 0)
                            return;

                        if (SelectedItem == null)
                        {
                            SelectedItem = Channels[Channels.Count - 1];
                        }
                        else
                        {
                            bool previous = false;
                            var previousCount = 1;

                            for (var i = Channels.Count-1; i >=0 ; i--)
                            {
                                if (previous)
                                {
                                    if (previousCount == step || i == 0)
                                    {
                                        SelectedItem = Channels[i];
                                        break;
                                    } else
                                    {
                                        previousCount++;
                                    }
                                }
                                else
                                {
                                    if (Channels[i] == SelectedItem)
                                    {
                                        previous = true;
                                    }
                                }
                            }
                        }
                    }
                    finally
                    {
                        _semaphoreSlim.Release();
                    };
                });
        }

        private Dictionary<string, ChannelItem> ChannelById
        {
            get
            {
                return _channelById;
            }
        }

        public string FontSizeForChannel
        {
            get
            {
                return GetScaledSize(16).ToString();
            }
        }

        public string FontSizeForInfoLabel
        {
            get
            {
                return GetScaledSize(12).ToString();
            }
        }

        public string FontSizeForChannelNumber
        {
            get
            {
                return GetScaledSize(11).ToString();
            }
        }

        public string FontSizeForTime
        {
            get
            {
                return GetScaledSize(11).ToString();
            }
        }

        public string FontSizeForChannelEPG
        {
            get
            {
                return GetScaledSize(12).ToString();
            }
        }

        public int HeightForChannelNameRow
        {
            get
            {
                return GetScaledSize(40);
            }
        }

        public int WidthForIcon
        {
            get
            {
                return GetScaledSize(40);
            }
        }

        public int HeightForChannelEPGRow
        {
            get
            {
                return GetScaledSize(30);
            }
        }

        public int HeightForTimeRow
        {
            get
            {
                return GetScaledSize(14);
            }
        }

        public string FontSizeForTitle
        {
            get
            {
                return GetScaledSize(22).ToString();
            }
        }

        public string FontSizeForDescription
        {
            get
            {
                return GetScaledSize(16).ToString();
            }
        }

        public string StatusLabel
        {
            get
            {
                if (IsBusy ||
                    (_lastRefreshDelay > 0 && _lastRefreshDelay <= 4 )) // only first few seconds
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

                if (!Config.Purchased)
                    status = "Verze zdarma. ";

                if (Channels.Count == 0)
                {
                    return $"{status}Není k dispozici žádný kanál";
                }
                else
                if (Channels.Count == 1)
                {
                    return $"{status}Načten 1 kanál";
                }
                else
                if ((Channels.Count >= 2) && (Channels.Count <= 4))
                {
                    return $"{status}Načteny {Channels.Count} kanály";
                }
                else
                {
                    return $"{status}Načteno {Channels.Count} kanálů";
                }
            }
        }

        private async Task Refresh()
        {
            _loggingService.Info($"Refresh channels & EPG");

            await RefreshChannels();
            await RefreshEPG();
        }

        private async Task RefreshChannels()
        {
            _loggingService.Info($"RefreshChannels");

            await CheckPurchase();

            try
            {
                string selectedChannelNumber = null;
                if (SelectedItem == null)
                {
                    selectedChannelNumber = Config.LastChannelNumber;
                }
                else
                {
                    selectedChannelNumber = SelectedItem.ChannelNumber;
                }

                OnPropertyChanged(nameof(StatusLabel));

                await _semaphoreSlim.WaitAsync();

                IsBusy = true;

                var channels = await _service.GetChannels();

                if (channels != null && channels.Count > 0)
                {
                    Channels.Clear();
                    AllNotFilteredChannels.Clear();
                    _channelById.Clear();

                    var channelByNumber = new Dictionary<string, ChannelItem>();

                    foreach (var ch in channels)
                    {
                        AllNotFilteredChannels.Add(ch);

                        if (Config.ChannelFilterGroup != "*" &&
                            Config.ChannelFilterGroup != null &&
                            Config.ChannelFilterGroup != ch.Group)
                            continue;

                        if (Config.ChannelFilterType != "*" &&
                            Config.ChannelFilterType != null &&
                            Config.ChannelFilterType != ch.Type)
                            continue;

                        if ((!String.IsNullOrEmpty(Config.ChannelFilterName)) &&
                            (Config.ChannelFilterName != "*") &&
                            !ch.Name.ToLower().Contains(Config.ChannelFilterName.ToLower()))
                            continue;

                        Channels.Add(ch);

                        if (!_channelById.ContainsKey(ch.Id)) 
                            _channelById.Add(ch.Id, ch); // for faster EPG refresh

                        channelByNumber.Add(ch.ChannelNumber, ch); // for channel selecting
                    }

                    if (!String.IsNullOrEmpty(selectedChannelNumber) && channelByNumber.ContainsKey(selectedChannelNumber))
                    {
                        SelectedItem = channelByNumber[selectedChannelNumber];
                    }
                    else if (Channels.Count > 0)
                    {
                        // selecting first channel
                        SelectedItem = Channels[0];
                    }
                }

            }
            finally
            {
                IsBusy = false;

                _semaphoreSlim.Release();

                OnPropertyChanged(nameof(StatusLabel));
                OnPropertyChanged(nameof(IsBusy));
                OnPropertyChanged(nameof(SelectedChannelEPGTitle));
                OnPropertyChanged(nameof(SelectedChannelEPGDescription));
                OnPropertyChanged(nameof(SelectedChannelEPGProgress));
                OnPropertyChanged(nameof(EPGProgressBackgroundColor));

            }

            if (Channels.Count == 0)
            {
                if (_lastRefreshDelay == 0)
                {
                    // first refresh
                    _lastRefreshDelay = 2;
                }
                else
                {
                    if (_lastRefreshDelay < 60)
                    {
                        _lastRefreshDelay *= 2;
                    }
                }

                // refresh again after _lastRefreshDelay seconds
                BackgroundCommandWorker.RunInBackground(RefreshCommand, 0, _lastRefreshDelay);
            }
            else
            {
                _lastRefreshDelay = 0;
            }

            if (_service.Status == StatusEnum.Logged)
            {
                // auto play?
                if (_firstRefresh)
                {
                    _firstRefresh = false;
                    await AutoPlay();
                }
            }
        }

        private async Task RefreshEPG()
        {
            if (!_service.EPGEnabled)
                return;

            _loggingService.Info($"RefreshEPG");


            var epgItemsCountRead = 0;
            try
            {
                OnPropertyChanged(nameof(StatusLabel));

                await _semaphoreSlim.WaitAsync();

                IsBusy = true;

                foreach (var channelItem in Channels)
                {
                    channelItem.ClearEPG();
                }

                var epg = await _service.GetEPG();

                if (epg != null)
                {
                    foreach (var ei in epg)
                    {
                        epgItemsCountRead++;

                        if (ChannelById.ContainsKey(ei.ChannelId))
                        {
                            // updating channel EPG

                            var ch = ChannelById[ei.ChannelId];
                            ch.AddEPGItem(ei);
                        }
                    }
                }
            }
            finally
            {
                IsBusy = false;

                _semaphoreSlim.Release();

                OnPropertyChanged(nameof(StatusLabel));
                OnPropertyChanged(nameof(IsBusy));
                OnPropertyChanged(nameof(SelectedChannelEPGTitle));
                OnPropertyChanged(nameof(SelectedChannelEPGDescription));
                OnPropertyChanged(nameof(SelectedChannelEPGProgress));
                OnPropertyChanged(nameof(EPGProgressBackgroundColor));
            }

            if (epgItemsCountRead == 0)
            {
                if (_lastRefreshDelay == 0)
                {
                    // refresh all after 3 seconds
                    BackgroundCommandWorker.RunInBackground(RefreshCommand, 0, 3);
                }
            }
        }

        private async Task AutoPlay()
        {
            _loggingService.Info("AutoPlay");

            if (String.IsNullOrEmpty(Config.AutoPlayChannelNumber) ||
                Config.AutoPlayChannelNumber == "-1")
                return;

            string channelNumber;

            if (Config.AutoPlayChannelNumber == "0")
            {
                if (string.IsNullOrEmpty(Config.LastChannelNumber))
                    return;

                channelNumber = Config.LastChannelNumber;
            } else
            {
                channelNumber = Config.AutoPlayChannelNumber;
            }

            foreach (var ch in Channels)
            {
                if (ch.ChannelNumber == channelNumber)
                {
                    SelectedItem = ch;
                    await Play();
                    break;
                }
            }
        }

        private async Task ResetConnection()
        {
            _loggingService.Info("Reseting connection");

            await _semaphoreSlim.WaitAsync();

            IsBusy = true;

            try
            {
                OnPropertyChanged(nameof(StatusLabel));

                await _service.ResetConnection();
            }
            finally
            {
                IsBusy = false;

                _semaphoreSlim.Release();

                OnPropertyChanged(nameof(StatusLabel));
                NotifyFontSizeChange();
            }
        }

        public void NotifyFontSizeChange()
        {
            OnPropertyChanged(nameof(FontSizeForChannel));
            OnPropertyChanged(nameof(FontSizeForChannelNumber));
            OnPropertyChanged(nameof(FontSizeForTime));
            OnPropertyChanged(nameof(FontSizeForTitle));
            OnPropertyChanged(nameof(FontSizeForDescription));
            OnPropertyChanged(nameof(FontSizeForChannelEPG));
            OnPropertyChanged(nameof(HeightForChannelNameRow));
            OnPropertyChanged(nameof(HeightForChannelEPGRow));
            OnPropertyChanged(nameof(HeightForTimeRow));
            OnPropertyChanged(nameof(FontSizeForInfoLabel));
            OnPropertyChanged(nameof(WidthForIcon));
        }

        public async Task CheckPurchase()
        {
            if (Config.Purchased || Config.DebugMode)
                return;

            _loggingService.Info($"Checking purchase");

            try
            {
                // contacting service

                var connected = await CrossInAppBilling.Current.ConnectAsync();

                if (!connected)
                {
                    _loggingService.Info($"Connection to AppBilling service failed");
                    //await _dialogService.Information("Nepodařilo se ověřit stav zaplacení plné verze.");
                    return;
                }

                // check InAppBillingPurchase
                var purchases = await CrossInAppBilling.Current.GetPurchasesAsync(ItemType.InAppPurchase);
                foreach (var purchase in purchases)
                {
                    if (purchase.ProductId == Config.PurchaseProductId &&
                        purchase.State == PurchaseState.Purchased)
                    {
                        Config.Purchased = true;

                        _loggingService.Debug($"Already purchased (InAppBillingPurchase)");

                        _loggingService.Debug($"Purchase AutoRenewing: {purchase.AutoRenewing}");
                        _loggingService.Debug($"Purchase Payload: {purchase.Payload}");
                        _loggingService.Debug($"Purchase PurchaseToken: {purchase.PurchaseToken}");
                        _loggingService.Debug($"Purchase State: {purchase.State}");
                        _loggingService.Debug($"Purchase TransactionDateUtc: {purchase.TransactionDateUtc}");
                        _loggingService.Debug($"Purchase ConsumptionState: {purchase.ConsumptionState}");

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

        public async Task Play()
        {
            if (SelectedItem == null)
                return;

            _loggingService.Info($"Playing selected channel {SelectedItem.Name}");

            await PlayStream(SelectedItem.Url);
        }
    }
}
