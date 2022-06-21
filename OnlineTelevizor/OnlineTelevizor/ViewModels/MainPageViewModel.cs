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
        public Command UpdateNotificationCommand { get; set; }

        private PlayingStateEnum _playingState = PlayingStateEnum.Stopped;

        public Command VideoLongPressCommand { get; set; }

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

        private DateTime _shutdownTime = DateTime.MinValue;

        private int _animePos = 2;
        private bool _animePosIncreasing = true;

        private bool _notifyRefreshStatus = false;

        public enum SelectedPartEnum
        {
            ChannelsList = 0,
            EPGDetail = 1,
            ToolBar = 2
        }

        public string SelectedToolbarItemName { get; set; } = null;

        private SelectedPartEnum _selectedPart = SelectedPartEnum.ChannelsList;

        public bool IsPortrait { get; set; } = true;
        public bool DoNotScrollToChannel { get; set; } = false;

        public Command RefreshCommand { get; set; }
        public Command RefreshCommandWithNotification { get; set; }

        public Command PlayCommand { get; set; }

        public Command RefreshChannelsCommand { get; set; }
        public Command RefreshEPGCommand { get; set; }
        public Command ResetConnectionCommand { get; set; }
        public Command CheckPurchaseCommand { get; set; }

        public Command LongPressCommand { get; set; }
        public Command ShortPressCommand { get; set; }

        public Command AnimeIconCommand { get; set; }

        public Command ShutdownTimerCommand { get; set; }

        public Command UpCommand { get; set; }
        public Command DownCommand { get; set; }
        public Command LeftCommand { get; set; }
        public Command RightCommand { get; set; }
        public Command OKCommand { get; set; }
        public Command BackCommand { get; set; }

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
            RefreshCommandWithNotification = new Command(async () => await RefreshWithNotification());

            CheckPurchaseCommand = new Command(async () => await CheckPurchase());

            RefreshEPGCommand = new Command(async () => await RefreshEPG());

            RefreshChannelsCommand = new Command(async () => await RefreshChannels());

            ResetConnectionCommand = new Command(async () => await ResetConnection());

            PlayCommand = new Command(async () => await PlaySelectedChannel());

            AnimeIconCommand = new Command(async () => await Anime());

            ShutdownTimerCommand = new Command(async () => await ShutdownTimer());

            LongPressCommand = new Command(LongPress);
            ShortPressCommand = new Command(ShortPress);

            VideoLongPressCommand = new Command(VideoLongPress);

            UpdateRecordNotificationCommand = new Command(async () => await UpdateRecordNotification());

            UpdateNotificationCommand = new Command(async () => await UpdateNotification());

            UpCommand = new Command(async (key) => await AnyKeyPressed("up"));
            DownCommand = new Command(async (key) => await AnyKeyPressed("down"));
            LeftCommand = new Command(async (key) => await AnyKeyPressed("left"));
            RightCommand = new Command(async (key) => await AnyKeyPressed("right"));

            OKCommand = new Command(async () => await AnyKeyPressed("enter"));
            BackCommand = new Command(async () => await AnyKeyPressed("escape"));

            StartBackgroundThreads();
        }

        public PlayingStateEnum PlayingState
        {
            get
            {
                return _playingState;
            }
            set
            {
                _playingState = value;
            }
        }

        private void StartBackgroundThreads()
        {
            // refreshing every hour with no start delay
            BackgroundCommandWorker.RegisterCommand(RefreshCommand, "RefreshCommand", 3600, 3600);

            // refreshing EPG every min with 60s start delay
            BackgroundCommandWorker.RegisterCommand(RefreshEPGCommand, "RefreshEPGCommand", 60, 60);

            // update record notification
            BackgroundCommandWorker.RegisterCommand(UpdateRecordNotificationCommand, "UpdateRecordNotificationCommand", 10, 5);

            // update playing notification
            BackgroundCommandWorker.RegisterCommand(UpdateNotificationCommand, "UpdateNotificationCommand", 10, 5);

            BackgroundCommandWorker.RegisterCommand(AnimeIconCommand, "AnimeIconCommand", 1, 1);

            BackgroundCommandWorker.RegisterCommand(ShutdownTimerCommand, "ShutdownTimerCommand", 1, 1);
        }

        public string TimerText
        {
            get
            {
                if (_shutdownTime == DateTime.MinValue)
                {
                    return string.Empty;
                }

                var totalSecsToShutDown = (_shutdownTime - DateTime.Now).TotalSeconds;

                var minutes = Math.Floor(totalSecsToShutDown / 60.0);
                var secs = Math.Floor(totalSecsToShutDown - minutes*60);

                if (minutes > 0 || secs > 0)
                {
                    return minutes.ToString("#0").PadLeft(2, '0') + ":" + secs.ToString("#0").PadLeft(2, '0');
                }

                return string.Empty;
            }
        }

        public bool TimerTextVisible
        {
            get
            {
                if (_shutdownTime == DateTime.MinValue)
                {
                    return false;
                } else
                {
                    return true;
                }
            }
        }

        public bool DebugArrowVisible
        {
            get
            {
#if ARROWS
                return true;
#else
                return false;
#endif
            }
        }

        private async Task AnyKeyPressed(string key)
        {
            MessagingCenter.Send(key, BaseViewModel.MSG_KeyMessage);
        }

        private async Task ShutdownTimer()
        {
            if (_shutdownTime == DateTime.MinValue)
            {
                return;
            }

            Device.BeginInvokeOnMainThread(() =>
            {
                OnPropertyChanged(nameof(TimerTextVisible));
                OnPropertyChanged(nameof(TimerText));
            });

            if (_shutdownTime < DateTime.Now)
            {
                MessagingCenter.Send<string>(string.Empty, BaseViewModel.MSG_StopPlayInternalNotificationAndQuit);
            }
        }

        private async Task Anime()
        {
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
                Device.BeginInvokeOnMainThread(() =>
                {
                    OnPropertyChanged(nameof(AudioIcon));
                });
            }
            catch {
            // UWP platform fix
            }
        }

        public async Task StopTimer()
        {
            if (_shutdownTime != DateTime.MinValue)
            {
                var confirm = await _dialogService.Confirm($"Zrušit časovač vypnutí?");
                if (confirm)
                {
                    _shutdownTime = DateTime.MinValue;
                    _loggingService.Info($"Timer stopped");
                }
            }

            OnPropertyChanged(nameof(TimerTextVisible));
            OnPropertyChanged(nameof(TimerText));
        }

        public async Task SetTimer(decimal minutesTimeout)
        {
            if (minutesTimeout == 0)
                return;

            var confirm = await _dialogService.Confirm($"Vypnout aplikaci za {minutesTimeout} minut?");
            if (confirm)
            {
                _shutdownTime = DateTime.Now.AddMinutes(Convert.ToDouble(minutesTimeout));

                MessagingCenter.Send($"Aplikace se vypne za {minutesTimeout} minut", BaseViewModel.MSG_ToastMessage);

                _loggingService.Info($"Timer: {minutesTimeout}");

                OnPropertyChanged(nameof(TimerTextVisible));
                OnPropertyChanged(nameof(TimerText));
            }
        }

        private async Task UpdateRecordNotification()
        {
            _loggingService.Info($"UpdateRecordNotification");

            await Task.Run( async () =>
            {
               try
               {
                    await _semaphoreSlim.WaitAsync();

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

                   MessagingCenter.Send<BaseViewModel, ChannelItem>(this, BaseViewModel.MSG_UpdateRecordNotificationMessage, channel);

               }
               catch (Exception ex)
               {
                   _loggingService.Error(ex);
               }
               finally
               {
                    _semaphoreSlim.Release();
               }
           });
        }

        private void _recordingBackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            _loggingService.Info($"_recordingBackgroundWorker_DoWork started");

            var outputFileName = Path.Combine(Config.OutputDirectory, $"{_recordingChannel.Name} {DateTime.Now.ToString("yyyy-MM-dd--HH-mm-ss")}.ts");

            using (var libvlc = new LibVLC())
            using (var mediaPlayer = new MediaPlayer(libvlc))
            {
                var media = new Media(libvlc, _recordingChannel.Url, FromType.FromLocation);

                media.AddOption(":sout=#file{dst=" + outputFileName + "}");
                media.AddOption(":sout-keep");

                // Start recording
                mediaPlayer.Play(media);

                do
                {
                    System.Threading.Thread.Sleep(500);

                    var freespaceGB = Convert.ToInt64(Config.UsableSpace / 1000000000);

                    if (freespaceGB < 1)
                    {
                        throw new Exception("Nedosatatek volného místa");
                    }

                } while (IsRecording);

                mediaPlayer.Stop();
            }
        }

        public async Task ShowPopupMenu(ChannelItem item = null)
        {
            if (item == null)
                item = PlayingChannel;

            var itmName = item == null ? String.Empty : item.Name;
            _loggingService.Info($"ShowPopupMenu: {itmName}");

            string optionCancel = "Zpět";

            string optionPlay = "Spustit";
            string optionStop = "Stop";
            string optionPlayInPreview = "Spustit v náhledu";
            string optionPlayInFullscreen = "Spustit na celou obrazovku";
            string optionCast = "Odeslat ..";
            string optionStopCast = "Zastavit odesílání";
            string optionDetail = "Zobrazit detail ..";
            string optionClosePreview = "Stop";

            string optionToggleAudioStream = "Změnit zvukovou stopu";
            string optionToggleSubtitleTrack = "Titulky";

            string optionAddToFav = "Přidat k oblíbeným";
            string optionRemoveFromFav = "Odebrat z oblíbených";

            string optionRecord = "Nahrávat do souboru ..";
            string optionStopRecord = "Zastavit nahrávání";

            string optionSetTimer = "Nastavit časovač vypnutí ..";
            string optionStopTimer = "Zrušit časovač vypnutí ..";

            string optionStopApp = "Ukončit aplikaci";

            var actions = new List<string>();

            if (item != null && item != PlayingChannel)
            {
                actions.Add(optionPlay);
            }

            if (item != null && item == PlayingChannel)
            {
                if (PlayingState == PlayingStateEnum.PlayingInternal)
                {
                    actions.Add(optionPlayInPreview);
                }
                if (PlayingState == PlayingStateEnum.PlayingInPreview)
                {
                    actions.Add(optionPlayInFullscreen);
                }
            }

            if (item != null && item == PlayingChannel)
            {
                actions.Add(optionStop);
            }

            if (PlayingChannel != null &&
                (
                PlayingState == PlayingStateEnum.PlayingInPreview ||
                PlayingState == PlayingStateEnum.PlayingInternal)
                )
            {
                actions.Add(optionToggleAudioStream);

                if (_service.SubtitlesEnabled)
                {
                    actions.Add(optionToggleSubtitleTrack);
                }
            }

            if (item != null)
            {
                actions.Add(optionDetail);
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

            if (PlayingState == PlayingStateEnum.PlayingInPreview)
            {
                actions.Add(optionClosePreview);
            }

            if (item != null)
            {
                if (Config.FavouriteChannelNames.Contains(item.Name.Replace(";", ",")))
                {
                    actions.Add(optionRemoveFromFav);
                }
                else
                {
                    actions.Add(optionAddToFav);
                }
            }

            if (_shutdownTime == DateTime.MinValue)
            {
                actions.Add(optionSetTimer);
            } else
            {
                actions.Add(optionStopTimer);
            }

            actions.Add(optionStopApp);

            var title = item == null ? "Menu" : (item as ChannelItem).Name;

            if (Device.OS == TargetPlatform.Windows)
            {
                actions.Remove(optionRecord);
                actions.Remove(optionCast);
            }

            var selectedvalue = await _dialogService.Select(actions, title, optionCancel);

            if (selectedvalue == optionCancel)
            {
                return;
            }
            else if (selectedvalue == optionPlay)
            {
                await PlaySelectedChannel();
            }
            else if (selectedvalue == optionPlayInFullscreen)
            {
                await PlaySelectedChannel();
            }
            else if (selectedvalue == optionStop)
            {
                MessagingCenter.Send<string>(string.Empty, BaseViewModel.MSG_StopPlay);
            }
            else if (selectedvalue == optionPlayInPreview)
            {
                MessagingCenter.Send<string>(string.Empty, BaseViewModel.MSG_PlayInPreview);
            }
            else if (selectedvalue == optionDetail)
            {
                MessagingCenter.Send<MainPageViewModel>(this, BaseViewModel.MSG_ShowDetailMessage);
            }
            else if (selectedvalue == optionCast)
            {
                MessagingCenter.Send<MainPageViewModel>(this, BaseViewModel.MSG_ShowRenderers);
            }
            else if (selectedvalue == optionStopCast)
            {
                MessagingCenter.Send<MainPageViewModel>(this, BaseViewModel.MSG_StopCasting);
            }
            else if (selectedvalue == optionRecord)
            {
                await RecordChannel(true, false);
            }
            else if (selectedvalue == optionStopRecord)
            {
                await RecordChannel(false, false);
            }
            else if (selectedvalue == optionClosePreview)
            {
                MessagingCenter.Send<string>(string.Empty, BaseViewModel.MSG_StopPlay);
            }
            else if (selectedvalue == optionToggleAudioStream)
            {
                await AudioSubMenu();
            }
            else if (selectedvalue == optionToggleSubtitleTrack)
            {
                MessagingCenter.Send<string>(string.Empty, BaseViewModel.MSG_ToggleSubtitles);
            }
            else if (selectedvalue == optionStopApp)
            {
                var confirm = await _dialogService.Confirm($"Ukončit aplikaci?");
                if (confirm)
                    MessagingCenter.Send<string>(string.Empty, BaseViewModel.MSG_StopPlayInternalNotificationAndQuit);
            }
            else if (selectedvalue == optionAddToFav)
            {
                AddToFav(item);
            }
            else if (selectedvalue == optionRemoveFromFav)
            {
                RemoveFromFav(item);
            }
            else if (selectedvalue == optionSetTimer)
            {
                MessagingCenter.Send<MainPageViewModel>(this, BaseViewModel.MSG_ShowTimer);
            }
            else if (selectedvalue == optionStopTimer)
            {
                await StopTimer();
            }
        }

        public async Task AudioSubMenu()
        {
            if (PlayingChannel != null &&
                PlayingChannel.AudioTracks != null &&
                PlayingChannel.AudioTracks.Count > 0
                )
            {
                var menuTitleToTrackId = new Dictionary<string, int>(); // menu value -> audio track id
                var menu = new List<string>();

                foreach (var kvp in PlayingChannel.AudioTracks)
                {
                    var uniqueMenuTitle = kvp.Value;

                    if (menuTitleToTrackId.ContainsKey(uniqueMenuTitle))
                    {
                        var num = 1;
                        string name;

                        do
                        {
                            num++;
                            name = $"{kvp.Value} ({num})";
                        }
                        while (menuTitleToTrackId.ContainsKey(name));

                        uniqueMenuTitle = name;
                    }

                    menuTitleToTrackId.Add(uniqueMenuTitle, kvp.Key);
                    menu.Add(uniqueMenuTitle);
                }

                var selectedAudioTrack = await _dialogService.Select(menu, "Volba audio stopy", "Zrušit");

                if (selectedAudioTrack != null)
                {
                    MessagingCenter.Send<string>(menuTitleToTrackId[selectedAudioTrack].ToString(), BaseViewModel.MSG_ToggleAudioStreamId);
                }
            }
            else
            {
                MessagingCenter.Send<string>(string.Empty, BaseViewModel.MSG_ToggleAudioStream);
            }
        }

        public void ToggleFav()
        {
            _loggingService.Info("ToggleFav");

            var item = SelectedItemSafe;
            if (item != null)
            {
                if (Config.FavouriteChannelNames.Contains(item.Name.Replace(";", ",")))
                {
                    RemoveFromFav(item);
                }
                else
                {
                    AddToFav(item);
                }
            }
        }

        private void AddToFav(ChannelItem item)
        {
            if (item == null || item.Name == null)
                return;

            _loggingService.Info($"AddToFav: {item.Name}");

            var name = item.Name.Replace(";", ",");
            var fav = Config.FavouriteChannelNames;

            if (!fav.Contains(name))
            {
                fav.Add(name);
                Config.FavouriteChannelNames = fav;

                item.IsFav = true;
                item.NotifyStateChange();

                MessagingCenter.Send($"{item.Name} - přidáno mezi oblíbené", BaseViewModel.MSG_ToastMessage);
            }
        }

        private void RemoveFromFav(ChannelItem item)
        {
            if (item == null || item.Name == null)
                return;

            _loggingService.Info($"RemoveFromFav: {item.Name}");

            var name = item.Name.Replace(";", ",");
            var fav = Config.FavouriteChannelNames;

            if (fav.Contains(name))
            {
                fav.Remove(name);
                Config.FavouriteChannelNames = fav;

                item.IsFav = false;
                item.NotifyStateChange();

                MessagingCenter.Send($"{item.Name} - odebráno z oblíbených", BaseViewModel.MSG_ToastMessage);
            }
        }

        public async void LongPress(object item)
        {
            _loggingService.Info($"LongPress");

            if (item != null && item is ChannelItem)
            {
                var ch = item as ChannelItem;

                var alreadySelectedItem = SelectedItemSafe;
                if (alreadySelectedItem != ch)
                {
                    var rmb = DoNotScrollToChannel;
                    DoNotScrollToChannel = true;

                    SelectedItemSafe = ch;

                    DoNotScrollToChannel = rmb;
                } else
                {
                    await ShowPopupMenu(ch);
                }
            }
        }

        private void ShortPress(object item)
        {
            _loggingService.Info($"ShortPress");

            if (item != null && item is ChannelItem)
            {
                // select and play

                SelectedItemSafe = item as ChannelItem;

                if (SelectedItemSafe != null)
                {
                    _loggingService.Info($"Short press (channel {SelectedItemSafe.Name})");
                    Task.Run(async () => await PlaySelectedChannel());
                }
            }
        }

        private void VideoLongPress(object item)
        {
            _loggingService.Info($"VideoLongPress");

            Device.BeginInvokeOnMainThread(async () =>
            {
                await ShowPopupMenu(item as ChannelItem);
            });

            //MessagingCenter.Send<string>(string.Empty, BaseViewModel.ToggleAudioStream);
        }

        public async Task NotifyCastChannel(string channelNumber, bool castingStart)
        {
            _loggingService.Info($"NotifyCastChannel: {channelNumber},{castingStart}");

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

        public async Task RecordChannel(bool recordStart, bool skipConfirmation)
        {
            _loggingService.Info($"RecordChannel: {recordStart},{skipConfirmation}");

            var channel = SelectedItemSafe;
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

                if (!skipConfirmation)
                {
                    var confirm = await _dialogService.Confirm(msg);
                    if (!confirm)
                        return;
                }
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

                MessagingCenter.Send("Bylo zahájeno nahrávání", BaseViewModel.MSG_ToastMessage);

                if (Config.PlayOnBackground)
                {
                    MessagingCenter.Send<BaseViewModel, ChannelItem>(this, BaseViewModel.MSG_RecordNotificationMessage, channel);
                }
            } else
            {
                _recordingChannel = null;
                MessagingCenter.Send("Nahrávání bylo ukončeno", BaseViewModel.MSG_ToastMessage);

                if (Config.PlayOnBackground)
                {
                    MessagingCenter.Send<string>(string.Empty, BaseViewModel.MSG_StopRecordNotificationMessage);
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
                try
                {
                    _semaphoreSlim.WaitAsync();

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
                finally
                {
                    _semaphoreSlim.Release();
                };
            }
        }

        public bool StandingOnEnd
        {
            get
            {
                try
                {
                    _semaphoreSlim.WaitAsync();

                    var item = SelectedItem;

                    if (item == null)
                        return true;

                    ChannelItem lastChannel = null;
                    foreach (var ch in Channels)
                    {
                        lastChannel = ch;
                    }

                    if (lastChannel == item)
                        return true;

                    return false;

                }
                finally
                {
                    _semaphoreSlim.Release();
                };
            }
        }

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

        public string SelectedChannelEPGTitle
        {
            get
            {
                var item = SelectedItemSafe;
                if (item == null || item.CurrentEPGItem == null)
                    return String.Empty;

                return item.CurrentEPGItem.Title;
            }
        }

        public string SelectedChannelEPGTimeStart
        {
            get
            {
                var item = SelectedItemSafe;
                if (item == null)
                    return String.Empty;

                return item.EPGTimeStart;
            }
        }

        public string SelectedChannelEPGTimeFinish
        {
            get
            {
                var item = SelectedItemSafe;
                if (item == null)
                    return String.Empty;

                return item.EPGTimeFinish;
            }
        }

        public string ViewPreviewDescription
        {
            get
            {
                var item = SelectedItemSafe;
                if (item == null)
                    return String.Empty;

                if (item.CurrentEPGItem == null)
                {
                    return item.Name;
                }

                return $"{item.Name} - {item.CurrentEPGItem.Title}";
            }
        }

        public double SelectedChannelEPGProgress
        {
            get
            {
                var item = SelectedItemSafe;
                if (item == null || item.CurrentEPGItem == null)
                    return 0;

                return item.CurrentEPGItem.Progress;
            }
        }

        public Color EPGProgressBackgroundColor
        {
            get
            {
                var item = SelectedItemSafe;
                if (item == null || item.CurrentEPGItem == null)
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
            _loggingService.Info($"NotifyToolBarChange");

            Device.BeginInvokeOnMainThread(() =>
            {
                OnPropertyChanged(nameof(ToolbarItemFilterIcon));
                OnPropertyChanged(nameof(ToolbarItemHelpIcon));
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
            _loggingService.Info("UpdateNotification");

            if (PlayingChannel != null)
            {
                MessagingCenter.Send<MainPageViewModel, ChannelItem>(this, BaseViewModel.MSG_UpdateInternalNotification, PlayingChannel);
            }
        }

        private async Task UpdateSelectedChannelEPGDescription()
        {
            var item = SelectedItemSafe;
            if (item == null || item.CurrentEPGItem == null)
            {
                SelectedChannelEPGDescription = String.Empty;
                return;
            }

            SelectedChannelEPGDescription = await _service.GetEPGItemDescription(item.CurrentEPGItem);
        }

        public string SelectedChannelEPGDescription
        {
            get
            {
                return _selectedChannelEPGDescription.Trim();
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

        public ChannelItem SelectedItemSafe
        {
            get
            {
                _semaphoreSlim.WaitAsync();
                try
                {
                    return _selectedItem;
                }
                finally
                {
                    _semaphoreSlim.Release();
                };
            }
            set
            {
                _semaphoreSlim.WaitAsync();
                try
                {
                    SelectedItem = value;
                }
                finally
                {
                    _semaphoreSlim.Release();
                };
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
                OnPropertyChanged(nameof(SelectedChannelEPGTimeStart));
                OnPropertyChanged(nameof(SelectedChannelEPGTimeFinish));
                OnPropertyChanged(nameof(SelectedItem));

                Task.Run(async () => await UpdateSelectedChannelEPGDescription());
            }
        }

        public async Task SelectNextChannel(int step = 1)
        {
            _loggingService.Info($"Selecting next channel (step {step})");

            await Task.Run(
                async () =>
                {
                    await _semaphoreSlim.WaitAsync();
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
                                    if (nextCount == step || i == Channels.Count - 1)
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

            await Task.Run(
                async () =>
                {
                    await _semaphoreSlim.WaitAsync();

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

        public string FontSizeForTimer
        {
            get
            {
                return GetScaledSize(15).ToString();
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

        public string FontSizeForEPGTitle
        {
            get
            {
                return GetScaledSize(18).ToString();
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
                    case StatusEnum.GeneralError: return $"IPTV není dostupná";
                    case StatusEnum.ConnectionNotAvailable: return $"Chyba připojení";
                    case StatusEnum.NotInitialized: return "";
                    case StatusEnum.EmptyCredentials: return "Nevyplněny přihlašovací údaje";
                    case StatusEnum.Logged: return GetChannelsStatus;
                    case StatusEnum.LoginFailed: return $"Chyba přihlášení k IPTV";
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

                if (_notFilteredChannelsCount == 0)
                {
                    return $"{status}Není k dispozici žádný kanál";
                }

                if (_notFilteredChannelsCount != Channels.Count)
                {
                    return $"{status}Zobrazeno {Channels.Count} z {_notFilteredChannelsCount} kanálů";
                } else
                {
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
        }

        private async Task RefreshWithNotification()
        {
            _loggingService.Info($"RefreshWithNotification");

            await Refresh();

            if (_lastRefreshChannelsDelay == 0)
            {
                MessagingCenter.Send(StatusLabel, BaseViewModel.MSG_ToastMessage);
            }

            _loggingService.Info($"RefreshWithNotification finished");
        }

        private async Task Refresh()
        {
            _loggingService.Info($"Refresh");

            await CheckPurchase();

            if (await CheckEmptyCredentials())
            {
                MessagingCenter.Send<MainPageViewModel>(this, BaseViewModel.MSG_ShowConfiguration);
                return;
            }

            await RefreshChannels();
            var epgItemsRefreshedCount = await RefreshEPG();

            // auto refresh channels ?
            if ((_notFilteredChannelsCount == 0) &&
                 (_service.Status != StatusEnum.NotInitialized) &&
                 (_service.Status != StatusEnum.EmptyCredentials) &&
                 (_service.Status != StatusEnum.LoginFailed) &&
                 (_service.Status != StatusEnum.PairingFailed) &&
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
                BackgroundCommandWorker.RunCommandWithDelay(RefreshCommand, _lastRefreshChannelsDelay);
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
                   (_service.Status != StatusEnum.PairingFailed) &&
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
                BackgroundCommandWorker.RunCommandWithDelay(RefreshEPGCommand, _lastRefreshEPEGsDelay);
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

            _loggingService.Info($"Refresh finished");
        }

        private async Task RefreshChannels()
        {
            _loggingService.Info($"RefreshChannels");

            _notFilteredChannelsCount = 0;
            string selectedChannelNumber = null;
            string firstVisibleChannelNumber = null;
            bool selectedChannelNumberVisible = false;

            if (!_firstRefresh)
                DoNotScrollToChannel = true;

            try
            {
                if (SelectedItemSafe == null)
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

                var channels = await _service.GetChannels();

                Device.BeginInvokeOnMainThread(() =>
                {
                    IsBusy = true;
                });

                await _semaphoreSlim.WaitAsync();

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

                        if (Config.FavouriteChannelNames.Contains(ch.Name.Replace(";",",")))
                        {
                            ch.IsFav = true;
                        }

                        if (Config.ShowOnlyFavouriteChannels &&
                            !(Config.FavouriteChannelNames.Contains(ch.Name.Replace(";", ","))))
                        {
                            continue;
                        }

                        if (firstVisibleChannelNumber == null)
                        {
                            firstVisibleChannelNumber = ch.ChannelNumber;
                        }

                        if (selectedChannelNumber != null && ch.ChannelNumber == selectedChannelNumber)
                        {
                            selectedChannelNumberVisible = true;
                        }

                        Channels.Add(ch);

                        if (!_channelById.ContainsKey(ch.Id))
                            _channelById.Add(ch.Id, ch); // for faster EPG refresh
                    }
                }

            }
            finally
            {
                _semaphoreSlim.Release();

                Device.BeginInvokeOnMainThread( async () =>
                {
                    IsBusy = false;

                    OnPropertyChanged(nameof(SelectedItem));
                    OnPropertyChanged(nameof(IsBusy));
                    OnPropertyChanged(nameof(SelectedChannelEPGTitle));
                    OnPropertyChanged(nameof(SelectedChannelEPGProgress));
                    OnPropertyChanged(nameof(EPGProgressBackgroundColor));
                    OnPropertyChanged(nameof(SelectedChannelEPGTimeStart));
                    OnPropertyChanged(nameof(SelectedChannelEPGTimeFinish));

                    if (selectedChannelNumber != null)
                    {
                        if (selectedChannelNumberVisible)
                        {
                            await SelectChannelByNumber(selectedChannelNumber);
                        }
                        else if (firstVisibleChannelNumber != null)
                        {
                            await SelectChannelByNumber(firstVisibleChannelNumber);
                        }
                        else
                        {
                            SelectedItemSafe = null;
                        }
                    }

                });

                DoNotScrollToChannel = false;
            }

            _loggingService.Info($"RefreshChannels finished");
        }

        private async Task<int> RefreshEPG()
        {
            if (!_service.EPGEnabled)
                return 0;

            _loggingService.Info($"RefreshEPG");


            var epgItemsCountRead = 0;
            try
            {
                var epg = await _service.GetEPG();

                await _semaphoreSlim.WaitAsync();

                Device.BeginInvokeOnMainThread(async () =>
                {
                    IsBusy = true;
                });

                if (epg != null)
                {
                    foreach (var channelItem in Channels)
                    {
                        channelItem.ClearEPG();
                    }

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
                _semaphoreSlim.Release();

                Device.BeginInvokeOnMainThread(async () =>
                {
                    foreach (var channelItem in Channels)
                    {
                        channelItem.NotifyEPGChange();
                    }

                    IsBusy = false;

                    OnPropertyChanged(nameof(IsBusy));
                    OnPropertyChanged(nameof(SelectedChannelEPGTitle));
                    OnPropertyChanged(nameof(SelectedChannelEPGProgress));
                    OnPropertyChanged(nameof(EPGProgressBackgroundColor));
                    OnPropertyChanged(nameof(SelectedChannelEPGTimeStart));
                    OnPropertyChanged(nameof(SelectedChannelEPGTimeFinish));

                    await UpdateSelectedChannelEPGDescription();

                });
            }

            _loggingService.Info($"RefreshEPG finished");

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
                    SelectedItemSafe = ch;
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
            _loggingService.Info($"NotifyFontSizeChange");

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
            OnPropertyChanged(nameof(FontSizeForTimer));
        }

        public async Task PlaySelectedChannel()
        {
            var item = SelectedItemSafe;

            if (item == null)
                return;

            _loggingService.Info($"Playing selected channel {item.Name}");

            await Play(item);
        }

        public async Task<bool> CheckEmptyCredentials()
        {

            if (!_emptyCredentialsChecked && EmptyCredentials)
            {
                await _dialogService.ConfirmSingleButton("Nejsou vyplněny přihlašovací údaje" +
                    Environment.NewLine +
                    Environment.NewLine +
                    "Pro sledování živého vysílání je nutné být uživatelem SledovaniTV, Kuki nebo O2 TV a v nastavení musí být vyplněny odpovídající přihlašovací údaje k těmto službám.",
                    "Online Televizor", "Přejít do nastavení");

                _emptyCredentialsChecked = true;
                return true;
            }

            _emptyCredentialsChecked = true;
            return false;
        }

        public string ToolbarItemHelpIcon
        {
            get
            {
                if (SelectedToolbarItemName == "ToolbarItemHelp")
                    return "HelpSelected.png";

                return "Help.png";
            }
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
