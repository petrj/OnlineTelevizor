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
using System.Threading;
using Plugin.InAppBilling;
using TVAPI;

namespace OnlineTelevizor.ViewModels
{
    public class MainPageViewModel : BaseViewModel
    {
        private TVService _service;
        private static SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

        public ObservableCollection<ChannelItem> Channels { get; set; } = new ObservableCollection<ChannelItem>();
        public ObservableCollection<ChannelItem> AllNotFilteredChannels { get; set; } = new ObservableCollection<ChannelItem>();

        public bool IsCasting { get; set; }  = false;

        private Dictionary<string, ChannelItem> _channelById { get; set; } = new Dictionary<string, ChannelItem>();

        private ChannelItem _selectedItem;
        private bool _firstRefresh = true;
        private int _lastRefreshChannelsDelay = 0;
        private int _lastRefreshEPEGsDelay = 0;
        private int _notFilteredChannelsCount = 0;
        private bool _emptyCredentialsChecked = false;

        private string _selectedChannelEPGDescription = String.Empty;

        public enum SelectedPartEnum
        {
            ChannelsList = 0,
            EPGDetail = 1
        }

        private SelectedPartEnum _selectedPart = SelectedPartEnum.ChannelsList;

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

            // refreshing every hour with no start delay
            BackgroundCommandWorker.RunInBackground(RefreshCommand, 3600, 0);

            // refreshing EPG every min with 60s start delay
            BackgroundCommandWorker.RunInBackground(RefreshEPGCommand, 60, 60);
        }

        public async Task LongPressAction(ChannelItem item)
        {
            SelectedItem = item as ChannelItem;

            string optionCancel = "Zpět";
            string optionPlay = "Spustit ..";
            string optionCast = "Odeslat ..";
            string optionStopCast = "Zastavit odesílání";
            string optionDetail = "Zobrazit detail ..";

            var actions = new List<string>() { optionPlay };

            if (IsCasting)
            {
                actions.Add(optionStopCast);
            }
            else
            {
                actions.Add(optionCast);
            }

            if (IsPortrait)
            {
                actions.Add(optionDetail);
            }

            var selectedvalue = await _dialogService.Select(actions, (item as ChannelItem).Name, optionCancel);

            if (selectedvalue == optionCancel)
            {
                return;
            }
            else if (selectedvalue == optionPlay)
            {
                await Play();
            }
            else if (selectedvalue == optionDetail)
            {
                MessagingCenter.Send<MainPageViewModel>(this, BaseViewModel.ShowDetailMessage);
            }
            else if (selectedvalue == optionCast)
            {
                MessagingCenter.Send<MainPageViewModel>(this, BaseViewModel.ShowRenderers);
            }
            else if (selectedvalue == optionStopCast)
            {
                MessagingCenter.Send<MainPageViewModel>(this, BaseViewModel.StopCasting);
            }
        }

        public async void LongPress(object item)
        {
            if (item != null && item is ChannelItem)
            {
                await LongPressAction(item as ChannelItem);
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

            await Task.Run(
                async () =>
                {
                    try
                    {
                        await _semaphoreSlim.WaitAsync();

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

        public SelectedPartEnum SelectedPart
        {
            get
            {
                return _selectedPart;
            }
            set
            {
                _selectedPart = value;

                OnPropertyChanged(nameof(EPGDescriptionBackgroundColor));
            }
        }

        public Color EPGDescriptionBackgroundColor
        {
            get
            {
                if (_selectedPart == SelectedPartEnum.ChannelsList)
                    return Color.Black;

                return Color.FromHex("005996");
            }
        }

        private async Task UpdateSelectedChannelEPGDescription()
        {
            if (SelectedItem == null || SelectedItem.CurrentEPGItem == null)
            {
                SelectedChannelEPGDescription = String.Empty;
                return;
            }

            SelectedChannelEPGDescription = await _service.GetEPGItemDescription(SelectedItem.CurrentEPGItem);
        }

        public string SelectedChannelEPGDescription
        {
            get
            {
                return _selectedChannelEPGDescription;
            }
            set
            {
                _selectedChannelEPGDescription = value;
                OnPropertyChanged(nameof(SelectedChannelEPGDescription));
            }
        }

        public bool QualityFilterEnabled
        {
            get
            {
                return _service.QualityFilterEnabled;
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

                OnPropertyChanged(nameof(SelectedChannelEPGTitle));
                OnPropertyChanged(nameof(SelectedChannelEPGProgress));
                OnPropertyChanged(nameof(EPGProgressBackgroundColor));
                OnPropertyChanged(nameof(SelectedItem));

                Task.Run(async () => await UpdateSelectedChannelEPGDescription());
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

        public string FontSizeForNextTitle
        {
            get
            {
                return GetScaledSize(10).ToString();
            }
        }

        public string FontSizeForChannelEPG
        {
            get
            {
                return GetScaledSize(14).ToString();
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
                return GetScaledSize(15);
            }
        }

        public int HeightForNextTitleRow
        {
            get
            {
                return GetScaledSize(20);
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
                    (_lastRefreshChannelsDelay > 0 && _lastRefreshChannelsDelay <= 5 )) // only first few seconds
                {
                    return "Aktualizace kanálů...";
                }

                switch (_service.Status)
                {
                    case StatusEnum.GeneralError: return $"Služba není dostupná";
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
                    if (_notFilteredChannelsCount == 0)
                    {
                        return $"{status}"; // awaiting refresh ....
                    }
                    else
                    {
                        return $"{status}Není k dispozici žádný kanál";
                    }
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

            await CheckPurchase();

            await CheckEmptyCredentials();

            await RefreshChannels();
            var epgItemsRefreshedCount = await RefreshEPG();

            // auto refresh channels ?
            if ((_notFilteredChannelsCount == 0) &&
                 (_service.Status != StatusEnum.NotInitialized) &&
                 (_service.Status != StatusEnum.EmptyCredentials) &&
                 (_service.Status != StatusEnum.LoginFailed) &&
                 (_lastRefreshChannelsDelay < 3600)
               )
            {
                if (_lastRefreshChannelsDelay == 0)
                {
                    // first refresh
                    _lastRefreshChannelsDelay = 2;
                }
                else
                {
                    _lastRefreshChannelsDelay *= 2;
                }

                // refresh again after _lastRefreshChannelsDelay seconds
                BackgroundCommandWorker.RunInBackground(RefreshCommand, 0, _lastRefreshChannelsDelay);
            }
            else
            {
                _lastRefreshChannelsDelay = 0;
            }

            // auto refresh epg?
            if (
                (epgItemsRefreshedCount == 0) &&
                   (_service.Status != StatusEnum.NotInitialized) &&
                   (_service.Status != StatusEnum.EmptyCredentials) &&
                   (_service.Status != StatusEnum.LoginFailed) &&
                   (_lastRefreshEPEGsDelay < 60)
                   )
            {
                if (_lastRefreshEPEGsDelay == 0)
                {
                    // first refresh
                    _lastRefreshEPEGsDelay = 2;
                }
                else
                {
                    _lastRefreshEPEGsDelay *= 2;
                }

                // refresh again after _lastRefreshEPEGsDelay seconds
                BackgroundCommandWorker.RunInBackground(RefreshEPGCommand, 0, _lastRefreshEPEGsDelay);
            } else
            {
                _lastRefreshEPEGsDelay = 0;
            }

            if (_service.Status == StatusEnum.Logged && Channels.Count>0)
            {
                // auto play?
                if (_firstRefresh)
                {
                    _firstRefresh = false;
                    await AutoPlay();
                }
            }
        }

        private async Task RefreshChannels()
        {
            _loggingService.Info($"RefreshChannels");
            _notFilteredChannelsCount = 0;
            string selectedChannelNumber = null;

            if (!_firstRefresh)
                DoNotScrollToChannel = true;

            try
            {
                if (SelectedItem == null)
                {
                    selectedChannelNumber = Config.LastChannelNumber;
                }
                else
                {
                    selectedChannelNumber = SelectedItem.ChannelNumber;
                }

                if (selectedChannelNumber == null)
                {
                    selectedChannelNumber = "1";
                }

                OnPropertyChanged(nameof(StatusLabel));

                await _semaphoreSlim.WaitAsync();

                IsBusy = true;

                var channels = await _service.GetChannels();

                if (channels != null && channels.Count > 0)
                {
                    _notFilteredChannelsCount = channels.Count;
                    Channels.Clear();
                    AllNotFilteredChannels.Clear();
                    _channelById.Clear();

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
                    }
                }

            }
            finally
            {
                IsBusy = false;

                _semaphoreSlim.Release();

                OnPropertyChanged(nameof(SelectedItem));
                OnPropertyChanged(nameof(StatusLabel));
                OnPropertyChanged(nameof(IsBusy));
                OnPropertyChanged(nameof(SelectedChannelEPGTitle));
                OnPropertyChanged(nameof(SelectedChannelEPGProgress));
                OnPropertyChanged(nameof(EPGProgressBackgroundColor));

                if (selectedChannelNumber != null)
                {
                    await SelectChannelByNumber(selectedChannelNumber);
                }

                DoNotScrollToChannel = false;
            }
        }

        private async Task<int> RefreshEPG()
        {
            if (!_service.EPGEnabled)
                return 0;

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
                    foreach (var channelId in epg.Keys)
                    {
                        if (_channelById.ContainsKey(channelId))
                        {
                            foreach (var epgItem in epg[channelId])
                            {
                                if (epgItem.Finish < DateTime.Now)
                                    continue;

                                _channelById[channelId].AddEPGItem(epgItem);
                                epgItemsCountRead++;
                            }
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
                OnPropertyChanged(nameof(SelectedChannelEPGProgress));
                OnPropertyChanged(nameof(EPGProgressBackgroundColor));

                await UpdateSelectedChannelEPGDescription();
            }

            return epgItemsCountRead;
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
            OnPropertyChanged(nameof(FontSizeForNextTitle));
            OnPropertyChanged(nameof(FontSizeForTitle));
            OnPropertyChanged(nameof(FontSizeForDescription));
            OnPropertyChanged(nameof(FontSizeForChannelEPG));
            OnPropertyChanged(nameof(HeightForChannelNameRow));
            OnPropertyChanged(nameof(HeightForChannelEPGRow));
            OnPropertyChanged(nameof(HeightForTimeRow));
            OnPropertyChanged(nameof(HeightForNextTitleRow));
            OnPropertyChanged(nameof(FontSizeForInfoLabel));
            OnPropertyChanged(nameof(WidthForIcon));
        }



        public async Task Play()
        {
            if (SelectedItem == null)
                return;

            _loggingService.Info($"Playing selected channel {SelectedItem.Name}");

            await PlayStream(new MediaDetail()
            {
                MediaUrl = SelectedItem.Url,
                Title = SelectedItem.Name,
                Type = SelectedItem.Type,
                CurrentEPGItem = SelectedItem.CurrentEPGItem,
                NextEPGItem = SelectedItem.NextEPGItem,
                ChanneldID = SelectedItem.Id,
                LogoUrl = SelectedItem.LogoUrl
            });
        }

        public async Task CheckEmptyCredentials()
        {
            if (!_emptyCredentialsChecked && EmptyCredentials)
            {
                await _dialogService.ConfirmSingleButton("Nejsou vyplněny přihlašovací údaje" +
                    Environment.NewLine +
                    Environment.NewLine +
                    "Pro sledování živého vysílání je nutné být uživatelem SledovaniTV, Kuki nebo O2 TV a v nastavení musí být vyplněny odpovídající přihlašovací údaje k těmto službám.",
                    "Online Televizor", "Přejít do nastavení");

                MessagingCenter.Send<MainPageViewModel>(this, BaseViewModel.ShowConfiguration);
            }

            _emptyCredentialsChecked = true;
        }
    }
}
