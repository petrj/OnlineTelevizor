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
using System.ComponentModel;
using System.Net;
using System.IO;
using LibVLCSharp.Shared;

namespace OnlineTelevizor.ViewModels
{
    public class MainPageViewModel : BaseViewModel
    {
        private TVService _service;
        private static SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

        public ObservableCollection<ChannelItem> Channels { get; set; } = new ObservableCollection<ChannelItem>();
        public ObservableCollection<ChannelItem> AllNotFilteredChannels { get; set; } = new ObservableCollection<ChannelItem>();
        public Command UpdateRecordNotificationCommand { get; set; }

        public bool IsCasting { get; set; }  = false;
        public bool IsRecording { get; set; } = false;

        private Dictionary<string, ChannelItem> _channelById { get; set; } = new Dictionary<string, ChannelItem>();

        private BackgroundWorker _recordingBackgroundWorker = new BackgroundWorker();

        private ChannelItem _selectedItem;
        private bool _firstRefresh = true;
        private int _lastRefreshChannelsDelay = 0;
        private int _lastRefreshEPEGsDelay = 0;
        private int _notFilteredChannelsCount = 0;
        private bool _emptyCredentialsChecked = false;

        private string _selectedChannelEPGDescription = String.Empty;

        private ChannelItem _recordingChannel = null;
        private ChannelItem _castingChannel = null;

        private int _animePos = 2;
        private bool _animePosIncreasing = true;

        public enum SelectedPartEnum
        {
            ChannelsList = 0,
            EPGDetail = 1,
            ToolBar = 2
        }

        public string SelectedToolbarItemName { get; set; } = null;

        private SelectedPartEnum _selectedPart = SelectedPartEnum.ChannelsList;

        public TVService TVService
        {
            get
            {
                return _service;
            }
        }

        public string AudioIcon
        {
            get
            {
                return "Audio" + _animePos.ToString();
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

        public Command AnimeIconCommand { get; set; }

        public MainPageViewModel(ILoggingService loggingService, IOnlineTelevizorConfiguration config, IDialogService dialogService)
           : base(loggingService, config, dialogService)
        {
            _loggingService.Info("Initializing MainPageViewModel");

            _service = new TVService(loggingService, config);
            _loggingService = loggingService;
            _dialogService = dialogService;
            Config = config;

            _recordingBackgroundWorker.DoWork += _recordingBackgroundWorker_DoWork;

            RefreshCommand = new Command(async () => await Refresh());

            CheckPurchaseCommand = new Command(async () => await CheckPurchase());

            RefreshEPGCommand = new Command(async () => await RefreshEPG());

            RefreshChannelsCommand = new Command(async () => await RefreshChannels());

            ResetConnectionCommand = new Command(async () => await ResetConnection());

            PlayCommand = new Command(async () => await PlaySelectedChannel());

            AnimeIconCommand = new Command(async () => await Anime());

            LongPressCommand = new Command(LongPress);
            ShortPressCommand = new Command(ShortPress);

            UpdateRecordNotificationCommand = new Command(async () => await UpdateRecordNotification());

            // refreshing every hour with no start delay
            BackgroundCommandWorker.RunInBackground(RefreshCommand, 3600, 0);

            // refreshing EPG every min with 60s start delay
            BackgroundCommandWorker.RunInBackground(RefreshEPGCommand, 60, 60);

            // update record notification
            BackgroundCommandWorker.RunInBackground(UpdateRecordNotificationCommand, 10, 5);

            BackgroundCommandWorker.RunInBackground(AnimeIconCommand, 1, 1);
        }

        private async Task Anime()
        {
            /*
            if (_animePosIncreasing)
            {
                _animePos++;
                if (_animePos > 3)
                {
                    _animePos = 2;
                    _animePosIncreasing = !_animePosIncreasing;
                }
            }
            else
            {
                _animePos--;
                if (_animePos < 0)
                {
                    _animePos = 1;
                    _animePosIncreasing = !_animePosIncreasing;
                }
            }

            try
            {
                OnPropertyChanged(nameof(AudioIcon));
            }
            catch {
            // UWP platform fix
            }
        */
        }

        private async Task UpdateRecordNotification()
        {
            await Task.Run(() =>
            {
               try
               {
                   if (!IsRecording || _recordingChannel == null || !Config.PlayOnBackground)
                       return;

                   ChannelItem channel = null;

                   foreach (var ch in Channels)
                   {
                       if (ch.ChannelNumber == _recordingChannel.ChannelNumber)
                       {
                           channel = ch;
                           break;
                       }
                   }

                   if (channel == null)
                       return;

                   MessagingCenter.Send<BaseViewModel, ChannelItem>(this, BaseViewModel.UpdateRecordNotificationMessage, channel);

               }
               catch (Exception ex)
               {
                   _loggingService.Error(ex);
               }
           });
        }

        private void _recordingBackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            Stream stream = null;

            var outputFileName = Path.Combine(Config.OutputDirectory, $"{_recordingChannel.Name} {DateTime.Now.ToString("yyyy-MM-dd--HH-mm-ss")}.ts");

            try
            {
                using (var fileStream = new FileStream(outputFileName, FileMode.Create))
                {

                    int bytesToRead = 10240;
                    byte[] buffer = new Byte[bytesToRead];

                    var fileReq = HttpWebRequest.Create(_recordingChannel.Url);
                    var fileResp = (HttpWebResponse)fileReq.GetResponse();

                    stream = fileResp.GetResponseStream();

                    int length;
                    do
                    {
                        length = stream.Read(buffer, 0, bytesToRead);

                        fileStream.Write(buffer, 0, length);

                        //Clear the buffer
                        buffer = new Byte[bytesToRead];

                        var freespaceGB = Convert.ToInt64(Config.UsableSpace / 1000000000);

                        if (freespaceGB < 1)
                        {
                            throw new Exception("Nedosatatek volného místa");
                        }
                    } while (IsRecording && stream.CanRead);

                    fileStream.Flush();
                    fileStream.Close();
                }

            } catch (Exception ex)
            {
                _loggingService.Error(ex);
                RecordChannel(false);
            } finally
            {
                if (stream != null)
                {
                    //Close the input stream
                    stream.Close();
                    stream.Dispose();
                }
            }
        }

        public async Task ShowPopupMenu(ChannelItem item)
        {
            string optionCancel = "Zpět";

            string optionPlay = "Spustit ..";
            string optionCast = "Odeslat ..";
            string optionStopCast = "Zastavit odesílání";
            string optionDetail = "Zobrazit detail ..";

            string optionRecord = "Nahrávat do souboru ..";
            string optionStopRecord = "Zastavit nahrávání";

            string optionStopApp = "Ukončit aplikaci";

            var actions = new List<string>();

            if (item != null)
            {
                actions.Add(optionPlay);
            }

            if (item != null && !IsCasting && !IsRecording)
            {
                actions.Add(optionCast);
                actions.Add(optionRecord);
            }

            if (IsRecording)
            {
                actions.Add(optionStopRecord);
            }

            if (IsCasting)
            {
                actions.Add(optionStopCast);
            }

            if (item != null)
            {
                actions.Add(optionDetail);
            }

            actions.Add(optionStopApp);

            var selectedvalue = await _dialogService.Select(actions, (item as ChannelItem).Name, optionCancel);

            if (selectedvalue == optionCancel)
            {
                return;
            }
            else if (selectedvalue == optionPlay)
            {
                await PlaySelectedChannel();
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
            else if (selectedvalue == optionRecord)
            {
                await RecordChannel(true);
            }
            else if (selectedvalue == optionStopRecord)
            {
                await RecordChannel(false);
            }
            else if (selectedvalue == optionStopApp)
            {
                var confirm = await _dialogService.Confirm($"Ukončit aplikaci?");
                if (confirm)
                    MessagingCenter.Send<string>(string.Empty, BaseViewModel.StopPlayInternalNotificationAndQuit);
            }
        }

        public async void LongPress(object item)
        {
            if (item != null && item is ChannelItem)
            {
                var rmb = DoNotScrollToChannel;
                DoNotScrollToChannel = true;

                SelectedItem = item as ChannelItem;

                DoNotScrollToChannel = rmb;
            }
        }

        private void ShortPress(object item)
        {
            if (item != null && item is ChannelItem)
            {
                // select and play

                SelectedItem = item as ChannelItem;

                _loggingService.Info($"Short press (channel {SelectedItem.Name})");

                Task.Run(async () => await PlaySelectedChannel());
            }
        }

        public async Task NotifyCastChannel(string channelNumber, bool castingStart)
        {
            foreach (var ch in Channels)
            {
                if (ch.IsCasting)
                {
                    ch.IsCasting = false;
                    ch.NotifyStateChange();
                } else
                {
                    ch.IsCasting = false;
                }
            }

            IsCasting = castingStart;

            var channel = await SelectChannelByNumber(channelNumber);

            if (channel != null)
            {
                channel.IsCasting = castingStart;
                channel.NotifyStateChange();

                if (castingStart)
                {
                    _castingChannel = channel;
                } else
                {
                    _castingChannel = null;
                }
            }
        }

        public async Task RecordChannel(bool recordStart)
        {
            var channel = SelectedItem;
            if (channel == null)
                return;

            if (recordStart)
            {
                if (!Config.Purchased)
                {
                    await _dialogService.Information("Nahrávání je funkční jen v plné verzi");
                    return;
                }

                var freespace = Config.UsableSpace;
                var freespaceGB = Convert.ToInt64(freespace / 1000000000);

                if (freespaceGB < 1)
                {
                    await _dialogService.Information("Nahrávání není možné, je vyžadováno alespoň 1 GB volného místa");
                    return;
                }

                // confirm
                var msg = $"Soubor bude uložen do složky {Config.OutputDirectory}{System.Environment.NewLine}{System.Environment.NewLine}";

                if (!Config.PlayOnBackground)
                {
                    msg += $"Pozor, v konfiguraci není povolen běh na pozadí, po opuštění aplikace bude přehrávání ukončeno!{System.Environment.NewLine}{System.Environment.NewLine}";
                }

                msg += $"Zahájit nahrávání kanálu {channel.Name}?";

                var confirm = await _dialogService.Confirm(msg);
                if (!confirm)
                    return;
            }

            foreach (var ch in Channels)
            {
                if (ch.IsRecording)
                {
                    ch.IsRecording = false;
                    ch.NotifyStateChange();
                } else
                {
                    ch.IsRecording = false;
                }
            }

            IsRecording = recordStart;

            channel.IsRecording = recordStart;
            channel.NotifyStateChange();

            if (recordStart)
            {
                _recordingChannel = channel;
                _recordingBackgroundWorker.RunWorkerAsync();

                MessagingCenter.Send("Bylo zahájeno nahrávání", BaseViewModel.ToastMessage);

                if (Config.PlayOnBackground)
                {
                    MessagingCenter.Send<BaseViewModel, ChannelItem>(this, BaseViewModel.RecordNotificationMessage, channel);
                }
            } else
            {
                _recordingChannel = null;
                MessagingCenter.Send("Nahrávání bylo ukončeno", BaseViewModel.ToastMessage);

                if (Config.PlayOnBackground)
                {
                    MessagingCenter.Send<string>(string.Empty, BaseViewModel.StopRecordNotificationMessage);
                }
            }
        }

        public async Task<ChannelItem> SelectChannelByNumber(string number)
        {
            _loggingService.Info($"Selecting channel by number {number}");

            return await Task.Run(
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
                                return ch;
                            }
                        }

                        return null;
                    }
                    finally
                    {
                        _semaphoreSlim.Release();
                    };
                });
        }

        public bool StandingOnStart
        {
            get
            {
                if (SelectedItem == null)
                    return true;

                foreach (var ch in Channels)
                {
                    if (ch == SelectedItem)
                        return true;

                    return false;
                }

                return true;
            }
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

        public string ViewPreviewDescription
        {
            get
            {
                if (SelectedItem == null)
                    return String.Empty;

                if (SelectedItem.CurrentEPGItem == null)
                {
                    return SelectedItem.Name;
                }

                return $"{SelectedItem.Name} - {SelectedItem.CurrentEPGItem.Title}";
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
                NotifyToolBarChange();
            }
        }

        public void NotifyToolBarChange()
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                OnPropertyChanged(nameof(ToolbarItemFilterIcon));
                OnPropertyChanged(nameof(ToolbarItemQualityIcon));
                OnPropertyChanged(nameof(ToolbarItemInfoIcon));
                OnPropertyChanged(nameof(ToolbarItemSettingsIcon));
            });
        }

        public Color EPGDescriptionBackgroundColor
        {
            get
            {
                if (SelectedPart == SelectedPartEnum.EPGDetail )
                    return Color.FromHex("005996");

                return Color.Black;
            }
        }

        private async Task UpdateNotification()
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                if (PlayingChannel != null)
                {
                    MessagingCenter.Send<MainPageViewModel, ChannelItem>(this, BaseViewModel.UpdateInternalNotification, PlayingChannel);
                }
            });
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

        public bool VideoViewVisible
        {
            get
            {
                return true;
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
                if (_firstRefresh)
                {
                    // auto play?
                    await AutoPlay();
                }

                _firstRefresh = false;
            }

            // status notification
            MessagingCenter.Send(StatusLabel, BaseViewModel.ToastMessage);
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

                        if (IsRecording &&
                            _recordingChannel != null &&
                            _recordingChannel.ChannelNumber == ch.ChannelNumber)
                        {
                            ch.IsRecording = true;
                        }

                        if (IsCasting &&
                            _castingChannel!= null &&
                            _castingChannel.ChannelNumber == ch.ChannelNumber)
                        {
                            ch.IsCasting = true;
                        }

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
                    await PlaySelectedChannel();
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
                await _service.ResetConnection();
            }
            finally
            {
                IsBusy = false;

                _semaphoreSlim.Release();

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

        public async Task PlaySelectedChannel()
        {
            if (SelectedItem == null)
                return;

            _loggingService.Info($"Playing selected channel {SelectedItem.Name}");

            await Play(SelectedItem);
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

        public string ToolbarItemFilterIcon
        {
            get
            {
                if (SelectedToolbarItemName == "ToolbarItemFilter")
                    return "FilterSelected.png";

                return "Filter.png";
            }
        }

        public string ToolbarItemQualityIcon
        {
            get
            {
                if (SelectedToolbarItemName == "ToolbarItemQuality")
                    return "QualitySelected.png";

                return "Quality.png";
            }
        }

        public string ToolbarItemInfoIcon
        {
            get
            {
                if (SelectedToolbarItemName == "ToolbarItemInfo")
                    return "MenuSelected.png";

                return "Menu.png";
            }
        }

        public string ToolbarItemSettingsIcon
        {
            get
            {
                if (SelectedToolbarItemName == "ToolbarItemSettings")
                    return "SettingsSelected.png";

                return "Settings.png";
            }
        }
    }
}
