﻿using LoggerService;
using OnlineTelevizor.Models;
using OnlineTelevizor.Services;
using OnlineTelevizor.ViewModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.Forms;
using System.Threading;
using static OnlineTelevizor.ViewModels.MainPageViewModel;
using LibVLCSharp.Shared;

namespace OnlineTelevizor.Views
{
    public partial class MainPage : ContentPage, IOnKeyDown
    {
        private MainPageViewModel _viewModel;
        private DialogService _dialogService;
        private IOnlineTelevizorConfiguration _config;
        private ILoggingService _loggingService;
        private RemoteAccessService.RemoteAccessService _remoteAccessService;

        private FilterPage _filterPage = null;
        private CastRenderersPage _renderersPage;
        private TimerPage _timerPage;
        private SettingsPage _settingsPage = null;

        private DateTime _lastNumPressedTime = DateTime.MinValue;
        private DateTime _lastBackPressedTime = DateTime.MinValue;
        private DateTime _lastToggledAudioStreamTime = DateTime.MinValue;
        private DateTime _lastDetailclickedTime = DateTime.MinValue;
        private DateTime _lastActionOkMenuPopupTime = DateTime.MinValue;
        private bool _firstSelectionAfterStartup = false;
        private string _numberPressed = String.Empty;
        private bool _resumedWithoutReinitializingVideo = false;
        private bool _lastTimeHome = false;

        private LibVLC _libVLC = null;
        private MediaPlayer _mediaPlayer;
        private Media _media = null;

        private Size _lastAllocatedSize = new Size(-1, -1);

        private DateTime _lastSingleClicked = DateTime.MinValue;

        private ChannelItem[] _lastPlayedChannels = new ChannelItem[2];

        public Command CheckStreamCommand { get; set; }

        public string AppVersion { get; set; } = String.Empty;

        private List<string> _remoteDevicesConnected = new List<string>();

        public MainPage(ILoggingService loggingService, IOnlineTelevizorConfiguration config)
        {
            _config = config;
            _loggingService = loggingService;

            InitializeComponent();

            _dialogService = new DialogService(this);

            Core.Initialize();

            _libVLC = new LibVLC();
            _mediaPlayer = new MediaPlayer(_libVLC) { EnableHardwareDecoding = true };
            _mediaPlayer.Opening += _mediaPlayer_Opening;
            _mediaPlayer.Buffering += _mediaPlayer_Buffering;
            videoView.MediaPlayer = _mediaPlayer;

            _loggingService.Info($"Initializing MainPage");

            BindingContext = _viewModel = new MainPageViewModel(loggingService, config, _dialogService);

            ScrollViewChannelEPGDescription.Scrolled += ScrollViewChannelEPGDescription_Scrolled;
            Appearing += MainPage_Appearing;
            Disappearing += MainPage_Disappearing;

            ChannelsListView.ItemSelected += ChannelsListView_ItemSelected;
            ChannelsListView.Scrolled += ChannelsListView_Scrolled;

            PlayingState = PlayingStateEnum.Stopped;

            CheckStreamCommand = new Command(async () => await CheckStream());

            _remoteAccessService = new RemoteAccessService.RemoteAccessService(_loggingService);

            Reset();

            BackgroundCommandWorker.RegisterCommand(CheckStreamCommand, "CheckStreamCommand", 3, 2);

            RestartRemoteAccessService();
        }

        private void OnMessageReceived(RemoteAccessService.RemoteAccessMessage message)
        {
            if (message == null)
                return;

            var senderFriendlyName = message.GetSenderFriendlyName();
            if (!_remoteDevicesConnected.Contains(senderFriendlyName))
            {
                _remoteDevicesConnected.Add(senderFriendlyName);
                var msg = "Zahájeno vzdálené ovládání";
                if (!string.IsNullOrEmpty(senderFriendlyName))
                {
                    msg += $" ({senderFriendlyName})";
                }

                MessagingCenter.Send(msg, BaseViewModel.MSG_ToastMessage);
            }

            if (message.command == "keyDown")
            {
                MessagingCenter.Send(message.commandArg1, BaseViewModel.MSG_RemoteKeyAction);
            }
            if (message.command == "sendText")
            {
                OnTextSent(message.commandArg1);
            }
        }

        private void _mediaPlayer_Buffering(object sender, MediaPlayerBufferingEventArgs e)
        {
            _loggingService.Debug($"MediaPlayer_Buffering ({e.Cache})");
        }

        private void _mediaPlayer_Opening(object sender, EventArgs e)
        {
            _loggingService.Info($"_mediaPlayer_Opening");
        }

        ~MainPage()
        {
            BackgroundCommandWorker.UnregisterCommands(_loggingService);
        }

        public void SubscribeMessages()
        {
            _loggingService.Info($"Subscribing messages");

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_KeyAction, (key) =>
            {
                var longPress = false;
                if (key.StartsWith(BaseViewModel.LongPressPrefix))
                {
                    longPress = true;
                    key = key.Substring(BaseViewModel.LongPressPrefix.Length);
                }

                OnKeyDown(key, longPress);
            });

            MessagingCenter.Subscribe<MainPageViewModel>(this, BaseViewModel.MSG_ShowDetailMessage, (sender) =>
            {
                var detailPage = new ChannelDetailPage(_loggingService, _config, _dialogService, _viewModel.TVService);
                detailPage.Channel = _viewModel.SelectedItemSafe;

                Navigation.PushAsync(detailPage);
            });

            MessagingCenter.Subscribe<MainPageViewModel>(this, BaseViewModel.MSG_ShowRenderers, (sender) =>
            {
                var item = _viewModel.SelectedItemSafe;

                if (item == null)
                    return;

                if (_renderersPage == null)
                {
                    _renderersPage = new CastRenderersPage(_loggingService, _config);
                }

                _renderersPage.Channel = item;

                Navigation.PushAsync(_renderersPage);
            });

            MessagingCenter.Subscribe<MainPageViewModel>(this, BaseViewModel.MSG_ShowTimer, (sender) =>
            {
                if (IsPageOnTop(typeof(TimerPage)))
                    return;

                if (_timerPage == null)
                {
                    _timerPage = new TimerPage(_loggingService, _config, _dialogService);
                    _timerPage.Disappearing += _timerPage_Disappearing;
                }

                Navigation.PushAsync(_timerPage);
            });

            MessagingCenter.Subscribe<MainPageViewModel>(this, BaseViewModel.MSG_StopCasting, (sender) =>
            {
                if (_renderersPage != null)
                {
                    _renderersPage.StopCasting();
                }
            });

            MessagingCenter.Subscribe<BaseViewModel, ChannelItem>(this, BaseViewModel.MSG_PlayInternal, (sender, channel) =>
            {
                ActionPlay(channel);
            });

            MessagingCenter.Subscribe<MainPageViewModel>(this, BaseViewModel.MSG_ShowConfiguration, (sender) =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    ToolbarItemSettings_Clicked(this, null);
                });
            });

            MessagingCenter.Subscribe<BaseViewModel, ChannelItem>(this, BaseViewModel.MSG_CastingStarted, (sender, channel) =>
            {
                Task.Run(async () =>
                {
                    await _viewModel.NotifyCastChannel(channel.ChannelNumber, true);
                });
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_CastingStopped, (channelNumber) =>
            {
                Task.Run(async () =>
                {
                    await _viewModel.NotifyCastChannel(channelNumber, false);
                });
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_StopPlay, async (sender) =>
            {
                ActionStop(true);
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_PlayInPreview, async (sender) =>
            {
                ActionStop(false);
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_StopRecord, async (sender) =>
            {
                await Task.Run(async () =>
                {
                    await _viewModel.RecordChannel(false, false);
                });
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_ToggleAudioStream, async (sender) =>
            {
                ToggleAudioStream(null);
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_ToggleAudioStreamId, async (id) =>
            {
                ToggleAudioStream(id);
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_ToggleSubtitles, async (sender) =>
            {
                //
            });
        }

        public void UnsubscribeMessages()
        {
            _loggingService.Info($"Unsubscribing messages");

            MessagingCenter.Unsubscribe<MainPageViewModel>(this, BaseViewModel.MSG_KeyAction);
            MessagingCenter.Unsubscribe<MainPageViewModel>(this, BaseViewModel.MSG_ShowDetailMessage);
            MessagingCenter.Unsubscribe<MainPageViewModel>(this, BaseViewModel.MSG_ShowRenderers);
            MessagingCenter.Unsubscribe<MainPageViewModel>(this, BaseViewModel.MSG_ShowTimer);
            MessagingCenter.Unsubscribe<MainPageViewModel>(this, BaseViewModel.MSG_StopCasting);
            MessagingCenter.Unsubscribe<BaseViewModel, ChannelItem>(this, BaseViewModel.MSG_PlayInternal);
            MessagingCenter.Unsubscribe<MainPageViewModel>(this, BaseViewModel.MSG_ShowConfiguration);
            MessagingCenter.Unsubscribe<BaseViewModel, ChannelItem>(this, BaseViewModel.MSG_CastingStarted);
            MessagingCenter.Unsubscribe<string>(this, BaseViewModel.MSG_CastingStopped);
            MessagingCenter.Unsubscribe<string>(this, BaseViewModel.MSG_StopPlay);
            MessagingCenter.Unsubscribe<string>(this, BaseViewModel.MSG_StopRecord);
            MessagingCenter.Unsubscribe<string>(this, BaseViewModel.MSG_ToggleAudioStream);
            MessagingCenter.Unsubscribe<string>(this, BaseViewModel.MSG_ToggleSubtitles);

            if (_settingsPage != null)
            {
                _settingsPage.UnsubscribeMessages();
            }
        }

        private async void _timerPage_Disappearing(object sender, EventArgs e)
        {
            if (_timerPage.TimerMinutes>0)
            {
                await _viewModel.SetTimer(_timerPage.TimerMinutes);
            }
        }

        private void _renderersPage_Disappearing(object sender, EventArgs e)
        {
            if (_viewModel.SelectedItemSafe != null && _renderersPage != null)
            {
                _renderersPage.Channel.IsCasting = _renderersPage.IsCasting();
                _renderersPage.Channel.NotifyStateChange();
            }
        }

        public void RefreshGUI()
        {
            _loggingService.Info($"RefreshGUI");

            Device.BeginInvokeOnMainThread(() =>
            {
                switch (PlayingState)
                {
                    case PlayingStateEnum.PlayingInternal:

                        // turn off tool bar
                        NavigationPage.SetHasNavigationBar(this, false);

                        MessagingCenter.Send(String.Empty, BaseViewModel.MSG_EnableFullScreen);

                        StackLayoutEPGDetail.RowDefinitions[3].Height = new GridLength(30, GridUnitType.Star);
                        StackLayoutEPGDetail.RowDefinitions[4].Height = new GridLength(50, GridUnitType.Star);

                        LayoutGrid.ColumnDefinitions[0].Width = new GridLength(0, GridUnitType.Absolute);
                        LayoutGrid.ColumnDefinitions[1].Width = new GridLength(100, GridUnitType.Star);

                        // VideoStackLayout must be visible before changing Layout
                        var isVideoStackLayoutVisible = VideoStackLayout.IsVisible;
                        VideoStackLayout.IsVisible = true;
                        AbsoluteLayout.SetLayoutFlags(VideoStackLayout, AbsoluteLayoutFlags.All);
                        AbsoluteLayout.SetLayoutBounds(VideoStackLayout, new Rectangle(0, 0, 1, 1));
                        VideoStackLayout.IsVisible = isVideoStackLayoutVisible;

                        AbsoluteLayout.SetLayoutFlags(NoVideoStackLayout, AbsoluteLayoutFlags.All);
                        AbsoluteLayout.SetLayoutBounds(NoVideoStackLayout, new Rectangle(0, 1, 1, 0.35));

                        ClosePreviewVideoImage.IsVisible = false;
                        CloseVideoImage.IsVisible = false;
                        MinimizeVideoImage.IsVisible = false;

                        break;
                    case PlayingStateEnum.PlayingInPreview:

                        NavigationPage.SetHasNavigationBar(this, true);

                        if (!_config.Fullscreen)
                        {
                            MessagingCenter.Send(String.Empty, BaseViewModel.MSG_DisableFullScreen);
                        }

                        if (_viewModel.IsPortrait)
                        {
                            LayoutGrid.ColumnDefinitions[0].Width = new GridLength(100, GridUnitType.Star);
                            LayoutGrid.ColumnDefinitions[1].Width = new GridLength(0, GridUnitType.Absolute);

                            StackLayoutEPGDetail.RowDefinitions[3].Height = new GridLength(80, GridUnitType.Star);
                            StackLayoutEPGDetail.RowDefinitions[4].Height = new GridLength(0, GridUnitType.Absolute);
                        }
                        else
                        {
                            LayoutGrid.ColumnDefinitions[0].Width = new GridLength(50, GridUnitType.Star);
                            LayoutGrid.ColumnDefinitions[1].Width = new GridLength(50, GridUnitType.Star);

                            StackLayoutEPGDetail.RowDefinitions[3].Height = new GridLength(30, GridUnitType.Star);
                            StackLayoutEPGDetail.RowDefinitions[4].Height = new GridLength(50, GridUnitType.Star);
                        }

                        AbsoluteLayout.SetLayoutFlags(VideoStackLayout, AbsoluteLayoutFlags.All);
                        AbsoluteLayout.SetLayoutBounds(VideoStackLayout, new Rectangle(1, 1, 0.5, 0.35));

                        AbsoluteLayout.SetLayoutFlags(NoVideoStackLayout, AbsoluteLayoutFlags.All);
                        AbsoluteLayout.SetLayoutBounds(NoVideoStackLayout, new Rectangle(1, 1, 0.5, 0.35));

                        CheckStreamCommand.Execute(null);

                        ClosePreviewVideoImage.IsVisible = false;
                        CloseVideoImage.IsVisible = false;
                        MinimizeVideoImage.IsVisible = false;

                        break;
                    case PlayingStateEnum.Stopped:
                    case PlayingStateEnum.Casting:

                        NavigationPage.SetHasNavigationBar(this, true);

                        if (!_config.Fullscreen)
                        {
                            MessagingCenter.Send(String.Empty, BaseViewModel.MSG_DisableFullScreen);
                        }

                        if (_viewModel.IsPortrait)
                        {
                            LayoutGrid.ColumnDefinitions[0].Width = new GridLength(100, GridUnitType.Star);
                            LayoutGrid.ColumnDefinitions[1].Width = new GridLength(0, GridUnitType.Absolute);
                        }
                        else
                        {
                            LayoutGrid.ColumnDefinitions[0].Width = new GridLength(50, GridUnitType.Star);
                            LayoutGrid.ColumnDefinitions[1].Width = new GridLength(50, GridUnitType.Star);
                        }

                        StackLayoutEPGDetail.RowDefinitions[3].Height = new GridLength(80, GridUnitType.Star);
                        StackLayoutEPGDetail.RowDefinitions[4].Height = new GridLength(0, GridUnitType.Absolute);

                        VideoStackLayout.IsVisible = false;
                        NoVideoStackLayout.IsVisible = false;
                        ClosePreviewVideoImage.IsVisible = false;
                        CloseVideoImage.IsVisible = false;
                        MinimizeVideoImage.IsVisible = false;

                        break;
                }
            });
        }

        private void MainPage_Disappearing(object sender, EventArgs e)
        {
            _loggingService.Info($"MainPage_Disappearing");

            // workaround for turned off TV when playing
            try
            {
                VideoStackLayout.Children.Remove(videoView);
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
            }
        }

        private void SwipeGestureRecognizer_Swiped(object sender, SwipedEventArgs e)
        {
            if (e.Direction == SwipeDirection.Left)
            {
                ActionStop(true);
            } else
            {
                ActionStop(false);
            }
        }

        private void SwipeGestureRecognizer_Up(object sender, SwipedEventArgs e)
        {
            Task.Run(async () =>
            {
                if (PlayingState == PlayingStateEnum.PlayingInternal)
                {
                    await ActionKeyUp(1);
                }
                else if (PlayingState == PlayingStateEnum.PlayingInPreview)
                {
                    PlayingState = PlayingStateEnum.Stopped;
                }
            });
        }

        private void SwipeGestureRecognizer_Down(object sender, SwipedEventArgs e)
        {
            Task.Run(async () =>
            {
                if (PlayingState == PlayingStateEnum.PlayingInternal)
                {
                    await ActionKeyDown(1);
                }
                else if (PlayingState == PlayingStateEnum.PlayingInPreview)
                {
                    PlayingState = PlayingStateEnum.Stopped;
                }
            });
        }

        private void OnCloseVideoTapped(object sender, EventArgs e)
        {
            ActionStop(true);
        }

        private void OnMinimizeVideoTapped(object sender, EventArgs e)
        {
            ActionStop(false);
        }

        private async void OnTimerLabelTapped(object sender, EventArgs e)
        {
            await _viewModel.StopTimer();
        }

        private void OnSingleTapped(object sender, EventArgs e)
        {
            if ((_lastToggledAudioStreamTime != DateTime.MinValue) && (DateTime.Now - _lastToggledAudioStreamTime).TotalSeconds < 3)
            {
                return;
            }

            ActionTap(1);
        }

        public void OnDoubleTapped(object sender, EventArgs e)
        {
            ActionTap(2);
        }

        private async void MainPage_Appearing(object sender, EventArgs e)
        {
            _loggingService.Info($"MainPage_Appearing");

            if (!_viewModel.QualityFilterEnabled)
            {
                if (ToolbarItems.Contains(ToolbarItemQuality))
                {
                    ToolbarItems.Remove(ToolbarItemQuality);
                }
            }
            else
            {
                if (!ToolbarItems.Contains(ToolbarItemQuality))
                {
                    ToolbarItems.Insert(2, ToolbarItemQuality);
                }
            }

            if (Device.OS == TargetPlatform.Windows)
            {
                if (ToolbarItems.Contains(ToolbarItemHelp))
                {
                    ToolbarItems.Remove(ToolbarItemHelp);
                }
            }

            Resume();
        }

        private void ScrollViewChannelEPGDescription_Scrolled(object sender, ScrolledEventArgs e)
        {
            if (_viewModel.SelectedPart == SelectedPartEnum.ChannelsList)
            {
                // ScrollViewChannelEPGDescription got focus unexpectedly
                // hiding to lost focus
                // the only legal way to scroll EPG detail by keyobard is to change SelectedPartEnum to EPGDetail
                ScrollViewChannelEPGDescription.IsVisible = false;
                ScrollViewChannelEPGDescription.IsVisible = true;
            }
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            _loggingService.Info($"OnSizeAllocated: {width}/{height}");

            if (_viewModel == null)
                return;

            base.OnSizeAllocated(width, height);

            if (_lastAllocatedSize.Width == width &&
                _lastAllocatedSize.Height == height)
            {
                // no size changed
                return;
            }

            if (width>height)
            {
                _viewModel.IsPortrait = false;
            } else
            {
                _viewModel.IsPortrait = true;
            }

            _lastAllocatedSize.Width = width;
            _lastAllocatedSize.Height = height;

            _viewModel.NotifyToolBarChange();

            RefreshGUI();
        }

        private void ChannelsListView_Scrolled(object sender, ScrolledEventArgs e)
        {
            _loggingService.Debug($"ChannelsListView_Scrolled (ScrollY: {e.ScrollY})");

            // workaround for de-highlighting selected item after scroll on startup
            if (_firstSelectionAfterStartup)
            {
                _loggingService.Info($"ChannelsListView_Scrolled - highlighting channel");

                _viewModel.DoNotScrollToChannel = true;
                var item = _viewModel.SelectedItemSafe;
                _viewModel.SelectedItemSafe = null;
                _viewModel.SelectedItemSafe = item;
                _firstSelectionAfterStartup = false;
            }

        }

        private void ChannelsListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            _loggingService.Info($"ChannelsListView_ItemSelected");

            if (!_viewModel.DoNotScrollToChannel)
            {
                ChannelsListView.ScrollTo(_viewModel.SelectedItemSafe, ScrollToPosition.MakeVisible, false);
                _firstSelectionAfterStartup = true;
            }

            _viewModel.DoNotScrollToChannel = false;
        }

        public async void OnKeyDown(string key, bool longPress)
        {
            if (longPress)
            {
                _loggingService.Debug($"OnKeyDown - longpress {key}");
            } else
            {
                _loggingService.Debug($"OnKeyDown {key}");
            }

            var keyAction = KeyboardDeterminer.GetKeyAction(key);

            var stack = Navigation.NavigationStack;
            if (stack[stack.Count - 1].GetType() != typeof(MainPage))
            {
                // different page on navigation top

                var pageOnTop = stack[stack.Count - 1];

                if (pageOnTop is IOnKeyDown)
                {
                    (pageOnTop as IOnKeyDown).OnKeyDown(key, longPress);
                }

                return;
            }

            switch (keyAction)
            {
                case KeyboardNavigationActionEnum.Down:
                    await ActionKeyDown(longPress ? 10 : 1);
                    break;

                case KeyboardNavigationActionEnum.Up:
                    await ActionKeyUp(longPress ? 10 : 1);
                    break;

                case KeyboardNavigationActionEnum.Right:
                    await ActionKeyRight();
                    break;

                case KeyboardNavigationActionEnum.Left:
                    await ActionKeyLeft();
                    break;

                case KeyboardNavigationActionEnum.Back:
                    ActionBack(longPress);
                    break;

                case KeyboardNavigationActionEnum.OK:
                    await ActionKeyOK(longPress);
                    break;
            }

            switch (key.ToLower())
            {
                case "end":
                case "moveend":
                    await ActionFirstOrLast(false);
                    break;
                case "home":
                case "movehome":
                    await ActionFirstOrLast(true);
                    break;
                case "mediafastforward":
                case "mediaforward":
                case "pagedown":
                    await ActionKeyDown(10);
                    break;
                case "mediarewind":
                case "mediafastrewind":
                case "pageup":
                    await ActionKeyUp(10);
                    break;
                case "mediaplaypause":
                case "mediaplaystop":
                    if (PlayingState == PlayingStateEnum.Stopped)
                    {
                        ActionPlay(_viewModel.SelectedItemSafe);
                    }
                    else
                    {
                        ActionStop(true);
                    }
                    break;
                case "mediastop":
                case "mediaclose":
                    ActionStop(true);
                    break;
                case "f7":
                case "mediapause":
                case "forwarddel": // delete
                case "delete":
                case "altleft":
                case "minus":
                case "period":
                case "apostrophe":
                case "buttonselect":
                case "break": // pause
                    ActionStop(false);
                    break;
                case "buttonl2":
                case "info":
                case "guide":
                case "i":
                case "g":
                case "numpadadd":
                case "buttonthumbl":
                case "f1":
                case "f8":
                case "menu":
                case "tab":
                case "equals":
                case "slash":
                case "backslash":
                case "insert":
                case "tvcontentsmenu":
                    Detail_Clicked(this, null);
                    break;
                case "0":
                case "num0":
                case "number0":
                    HandleNumKey(0);
                    break;
                case "1":
                case "num1":
                case "number1":
                    HandleNumKey(1);
                    break;
                case "2":
                case "num2":
                case "number2":
                    HandleNumKey(2);
                    break;
                case "3":
                case "num3":
                case "number3":
                    HandleNumKey(3);
                    break;
                case "4":
                case "num4":
                case "number4":
                    HandleNumKey(4);
                    break;
                case "5":
                case "num5":
                case "number5":
                    HandleNumKey(5);
                    break;
                case "6":
                case "num6":
                case "number6":
                    HandleNumKey(6);
                    break;
                case "7":
                case "num7":
                case "number7":
                    HandleNumKey(7);
                    break;
                case "8":
                case "num8":
                case "number8":
                    HandleNumKey(8);
                    break;
                case "9":
                case "num9":
                case "number9":
                    HandleNumKey(9);
                    break;
                case "f5":
                case "numpad0":
                case "green":
                case "proggreen":
                case "f10":
                    Reset();
                    Refresh();
                    break;
                case "record":
                case "mediarecord":
                case "red":
                case "progred":
                case "f9":
                case "r":
                    Device.BeginInvokeOnMainThread(async () => await _viewModel.RecordChannel(!_viewModel.IsRecording, true));
                    break;
                case "yellow":
                case "progyellow":
                case "f11":
                case "l":
                    _viewModel.ToggleFav();
                    break;
                case "blue":
                case "progblue":
                case "f12":
                case "k":
                case "leftshift":
                case "shiftleft":
                    ToggleAudioStream(null);
                    break;
            }
        }

        public void OnTextSent(string text)
        {
            Device.BeginInvokeOnMainThread(delegate
                {
                    var stack = Navigation.NavigationStack;
                    if (stack[stack.Count - 1].GetType() != typeof(MainPage))
                    {
                        // different page on navigation top

                        var pageOnTop = stack[stack.Count - 1];

                        if (pageOnTop is IOnKeyDown)
                        {
                            (pageOnTop as IOnKeyDown).OnTextSent(text);
                        }

                        return;
                    }
            });
        }

        private Dictionary<int,string> GetAudioTracks()
        {
            var res = new Dictionary<int,string>();

            if (_mediaPlayer == null)
                return res;

            if (!_mediaPlayer.IsPlaying)
                return res;

            foreach (var desc in _mediaPlayer.AudioTrackDescription)
            {
                if (desc.Id >= 0)
                {
                    res.Add(desc.Id, desc.Name);
                }
            }

            return res;
        }

        private void ToggleAudioStream(string specificId)
        {
            _loggingService.Info($"ToggleAudioStream (id: {specificId})");

            if ((_lastToggledAudioStreamTime != DateTime.MinValue) && (DateTime.Now - _lastToggledAudioStreamTime).TotalSeconds < 3)
            {
                return;
            }

            _lastToggledAudioStreamTime = DateTime.Now;

            if (_mediaPlayer == null)
                return;

            if (_mediaPlayer.AudioTrackCount <= 1)
                return;

            var currentAudioTrack = _mediaPlayer.AudioTrack;
            if (currentAudioTrack == -1)
                return;

            var tracks = GetAudioTracks();

            if (tracks == null || tracks.Count == 0)
            {
                MessagingCenter.Send($"Nenalezena žádná zvuková stopa", BaseViewModel.MSG_ToastMessage);
                return;
            }

            var select = false;
            var selected = false;

            var firstAudioTrackId = -1;
            string firstAudioTrackName = null;

            string selectedName = null;
            int selectedId = -1;

            foreach (var desc in tracks)
            {
                if (firstAudioTrackId == -1)
                {
                    firstAudioTrackId = desc.Key;
                    firstAudioTrackName = desc.Value;
                }

                if (string.IsNullOrEmpty(specificId))
                {
                    // toggle next track
                    if (desc.Key == currentAudioTrack)
                    {
                        select = true;
                    }
                    else
                    {
                        if (select)
                        {
                            selectedName = desc.Value;
                            selectedId = desc.Key;
                            selected = true;
                            break;
                        }
                    }
                } else
                {
                    // toggle specific track
                    if (desc.Key.ToString() == specificId)
                    {
                        selectedName = desc.Value;
                        selectedId = desc.Key;
                        selected = true;
                        break;
                    }
                }
            }

            if (!selected)
            {
                selectedName = firstAudioTrackName;
                selectedId = firstAudioTrackId;
            }

            _mediaPlayer.SetAudioTrack(selectedId);

            if (string.IsNullOrEmpty(selectedName)) selectedName = $"# {selectedId}";

            MessagingCenter.Send($"Zvolena zvuková stopa {selectedName}", BaseViewModel.MSG_ToastMessage);

            _loggingService.Info($"Selected stream: {selectedName}");
        }

        private void HandleNumKey(int number)
        {
            _loggingService.Info($"HandleNumKey {number}");

            if ((DateTime.Now - _lastNumPressedTime).TotalSeconds > 2)
            {
                _lastNumPressedTime = DateTime.MinValue;
                _numberPressed = String.Empty;
            }

            _lastNumPressedTime = DateTime.Now;
            _numberPressed += number;

            MessagingCenter.Send(_numberPressed, BaseViewModel.MSG_ToastMessage);

            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                var numberPressedBefore = _numberPressed;

                Thread.Sleep(2000);

                if (numberPressedBefore == _numberPressed)
                {
                    Task.Run(async () =>
                    {
                        if (_numberPressed == "0")
                        {
                            switch (_viewModel.PlayingState)
                            {
                                case PlayingStateEnum.PlayingInternal:
                                    await ActionKeyLeft();
                                    break;
                                case PlayingStateEnum.PlayingInPreview:
                                    _viewModel.SelectedItem = _viewModel.PlayingChannel;
                                    ActionPlay(_viewModel.PlayingChannel);
                                    break;
                                case PlayingStateEnum.Stopped:
                                    if (_viewModel.StandingOnEnd)
                                    {
                                        await ActionFirstOrLast(true);
                                        _lastTimeHome = true;
                                    }
                                    else
                                    if (_viewModel.StandingOnStart)
                                    {
                                        await ActionFirstOrLast(false);
                                        _lastTimeHome = false;
                                    }
                                    else
                                    {
                                        await ActionFirstOrLast(_lastTimeHome);
                                        _lastTimeHome = !_lastTimeHome;
                                    }
                                    break;
                            };

                            return;
                        }

                        await _viewModel.SelectChannelByNumber(_numberPressed);

                        var item = _viewModel.SelectedItemSafe;

                        if (
                                (item != null) &&
                                (_numberPressed == item.ChannelNumber)
                           )
                        {
                            await _viewModel.Play(item);
                        }
                    });
                }

            }).Start();
        }

        public void Reset()
        {
            _loggingService.Info($"Reset");

            _viewModel.ResetConnectionCommand.Execute(null);
        }

        public PlayingStateEnum PlayingState
        {
            get
            {
                return _viewModel.PlayingState;
            }
            set
            {
                _viewModel.PlayingState = value;

                RefreshGUI();
            }
        }

        public void Resume()
        {
            _loggingService.Info($"Resume");

            if (_config.Fullscreen)
            {
                MessagingCenter.Send(String.Empty, BaseViewModel.MSG_EnableFullScreen);
            }

            // workaround for black screen after resume

            var radio = _viewModel.PlayingChannel != null
                                           && !string.IsNullOrEmpty(_viewModel.PlayingChannel.Type)
                                           && (_viewModel.PlayingChannel.Type.ToLower() == "radio")
                                           ? true
                                           : false;

            if (radio)
            {
                _resumedWithoutReinitializingVideo = true;
                return;
            }

            Device.BeginInvokeOnMainThread(() =>
            {
                float pos = 0;
                if (PlayingState == PlayingStateEnum.PlayingInPreview ||
                    PlayingState == PlayingStateEnum.PlayingInternal)
                {
                    pos = videoView.MediaPlayer.Position;
                    videoView.MediaPlayer.Stop();
                }

                VideoStackLayout.Children.Remove(videoView);
                VideoStackLayout.Children.Add(videoView);

                if (PlayingState == PlayingStateEnum.PlayingInPreview ||
                    PlayingState == PlayingStateEnum.PlayingInternal)
                {
                    videoView.MediaPlayer.Play();
                    videoView.MediaPlayer.Position = pos;
                }
            });
        }

        public void Refresh()
        {
            _viewModel.RefreshCommand.Execute(null);
        }

        public void RefreshWithnotification()
        {
            _viewModel.RefreshCommandWithNotification.Execute(null);
        }

        private void ShowJustPlayingNotification()
        {
            _loggingService.Debug($"ShowJustPlayingNotification");

            bool showCurrent;
            string msg;

            if (((DateTime.Now - _lastSingleClicked).TotalSeconds > 5) ||
                _viewModel.PlayingChannel == null ||
                _viewModel.PlayingChannel.NextEPGItem  == null)
            {
                showCurrent = true;
            }
            else
            {
                showCurrent = false;
            }

            if (showCurrent)
            {
                if (_viewModel.PlayingChannel != null)
                {
                    msg = $"\u25B6 {_viewModel.PlayingChannel.Name}";
                } else
                {
                    msg = $"\u25B6";
                }

                if (_viewModel.PlayingChannel != null &&
                _viewModel.PlayingChannel.CurrentEPGItem != null &&
                _viewModel.PlayingChannel.CurrentEPGItem.Start < DateTime.Now &&
                _viewModel.PlayingChannel.CurrentEPGItem.Finish > DateTime.Now &&
                !string.IsNullOrEmpty(_viewModel.PlayingChannel.CurrentEPGItem.Title))
                {
                    msg += $" - {_viewModel.PlayingChannel.CurrentEPGItem.Title}";
                }

                _lastSingleClicked = DateTime.Now;
            }
            else
            {
                if (_viewModel.PlayingChannel != null &&
                     _viewModel.PlayingChannel.NextEPGItem != null &&
                    !string.IsNullOrEmpty(_viewModel.PlayingChannel.NextEPGItem.Title))
                {
                    msg = $"-> {_viewModel.PlayingChannel.NextEPGItem.Start.ToString("HH:mm")} - {_viewModel.PlayingChannel.NextEPGItem.Title}";
                }
                else
                {
                    msg = $"\u25B6  {_viewModel.Title}";
                }

                _lastSingleClicked = DateTime.MinValue;
            }

            _loggingService.Info($"Test: {msg}");
            MessagingCenter.Send(msg, BaseViewModel.MSG_ToastMessage);
        }

        public void ActionPlay(ChannelItem channel)
        {
            if (channel == null)
                return;

            var channelName = channel == null ? String.Empty : channel.Name;
            _loggingService.Info($"ActionPlay: {channelName}");

            try
            {
                if (_config.InternalPlayer)
                {
                    if (_lastPlayedChannels[1] != channel)
                    {
                        _lastPlayedChannels[0] = _lastPlayedChannels[1];
                        _lastPlayedChannels[1] = channel;
                    }

                    Device.BeginInvokeOnMainThread(() =>
                    {
                        try
                        {
                            if (PlayingState == PlayingStateEnum.PlayingInPreview && _viewModel.PlayingChannel == channel)
                            {
                                PlayingState = PlayingStateEnum.PlayingInternal;
                                return;
                            }

                            if (PlayingState == PlayingStateEnum.PlayingInternal || PlayingState == PlayingStateEnum.PlayingInPreview)
                            {
                                videoView.MediaPlayer.Stop();
                            }

                            _media = new Media(_libVLC, channel.Url, FromType.FromLocation);

                            videoView.MediaPlayer = _mediaPlayer;

                            if (_resumedWithoutReinitializingVideo)
                            {
                                VideoStackLayout.Children.Remove(videoView);
                                VideoStackLayout.Children.Add(videoView);
                            }

                            NoVideoStackLayout.IsVisible = true;
                            VideoStackLayout.IsVisible = false;
                            AudioPlayingImage.IsVisible = true;

                            _mediaPlayer.Play(_media);

                            _viewModel.PlayingChannel = channel;

                            Task.Run(async () =>
                            {
                                await Task.Delay(500);
                                ShowJustPlayingNotification();
                            });

                            if (_config.PlayOnBackground)
                            {
                                MessagingCenter.Send<MainPage, ChannelItem>(this, BaseViewModel.MSG_PlayInternalNotification, channel);
                            }

                            PlayingState = PlayingStateEnum.PlayingInternal;
                        }
                        catch (Exception ex)
                        {
                            _loggingService.Error(ex, "PlayStream general error");
                            //MessagingCenter.Send($"Chyba: {ex.Message}", BaseViewModel.MSG_ToastMessage);
                        }
                    });
                }
                else
                {
                    MessagingCenter.Send(channel.Url, BaseViewModel.MSG_UriMessage);
                }
            } catch (Exception ex)
            {
                _loggingService.Error(ex, "PlayStream general error");
                //MessagingCenter.Send($"Chyba: {ex.Message}", BaseViewModel.MSG_ToastMessage);
            }
        }

        public void ActionTap(int count)
        {
            _loggingService.Info($"ActionTap: {count}");

            try
            {
                if (count == 1)
                {
                    if (PlayingState == PlayingStateEnum.PlayingInternal || PlayingState == PlayingStateEnum.PlayingInPreview)
                    {
                        ShowJustPlayingNotification();
                    }

                    if (PlayingState == PlayingStateEnum.PlayingInternal || PlayingState == PlayingStateEnum.PlayingInPreview)
                    {
                        Device.BeginInvokeOnMainThread(() =>
                        {
                            CloseVideoImage.IsVisible = MinimizeVideoImage.IsVisible = PlayingState == PlayingStateEnum.PlayingInternal;
                            ClosePreviewVideoImage.IsVisible = PlayingState == PlayingStateEnum.PlayingInPreview;
                        });

                        Task.Run(async () =>
                        {
                            await Task.Delay(5000);

                            Device.BeginInvokeOnMainThread(() =>
                            {
                                CloseVideoImage.IsVisible = false;
                                ClosePreviewVideoImage.IsVisible = false;
                                MinimizeVideoImage.IsVisible = false;
                            });
                        });
                    }
                }

                if (count == 2)
                {
                    if (PlayingState == PlayingStateEnum.PlayingInPreview)
                    {
                        PlayingState = PlayingStateEnum.PlayingInternal;

                        if  (_viewModel.SelectedItem.Id != _viewModel.PlayingChannel.Id)
                        {
                            _viewModel.SelectedItem = _viewModel.PlayingChannel;
                        }
                    }
                    else
                    if (PlayingState == PlayingStateEnum.PlayingInternal)
                    {
                        PlayingState = PlayingStateEnum.PlayingInPreview;
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "ActionTap general error");
                //MessagingCenter.Send($"Chyba: {ex.Message}", BaseViewModel.MSG_ToastMessage);
            }
        }

        public void ActionStop(bool force)
        {
            _loggingService.Info($"ActionStop: {force}");

            try
            {
                if (_config.InternalPlayer)
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        try
                        {

                            if (_media == null || videoView == null || videoView.MediaPlayer == null)
                                return;

                            if (!force && (PlayingState == PlayingStateEnum.PlayingInternal))
                            {
                                PlayingState = PlayingStateEnum.PlayingInPreview;
                            }
                            else
                            if (force || (PlayingState == PlayingStateEnum.PlayingInPreview))
                            {
                                videoView.MediaPlayer.Stop();
                                PlayingState = PlayingStateEnum.Stopped;
                                _viewModel.PlayingChannel = null;

                                MessagingCenter.Send<string>(string.Empty, BaseViewModel.MSG_StopPlayInternalNotification);
                            }
                        }
                        catch (Exception ex)
                        {
                            _loggingService.Error(ex, "ActionStop general error");
                            //MessagingCenter.Send($"Chyba: {ex.Message}", BaseViewModel.MSG_ToastMessage);
                        }
                    });
                }
                else
                {
                    PlayingState = PlayingStateEnum.Stopped;
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "ActionStop general error");
                //MessagingCenter.Send($"Chyba: {ex.Message}", BaseViewModel.MSG_ToastMessage);
            }
        }

        private async Task ActionKeyOK(bool longPress)
        {
            _loggingService.Info($"ActionKeyOK (LongPress: {longPress})");

            try
            {
                if (PlayingState == PlayingStateEnum.PlayingInternal)
                {
                    if (longPress)
                    {
                        // this event is called immediately after Navigation.PopAsync();
                        if (_lastActionOkMenuPopupTime != DateTime.MinValue && ((DateTime.Now - _lastActionOkMenuPopupTime).TotalSeconds < 2))
                        {
                            // ignoring this event
                            return;
                        }

                        _lastActionOkMenuPopupTime = DateTime.Now;

                        Device.BeginInvokeOnMainThread(async () =>
                        {
                            await _viewModel.ShowPopupMenu();
                        });
                    }
                    else
                    {
                        ShowJustPlayingNotification();
                    }
                    return;
                }

                if (_viewModel.SelectedPart == SelectedPartEnum.ChannelsList ||
                    _viewModel.SelectedPart == SelectedPartEnum.EPGDetail)
                {
                    ActionPlay(_viewModel.SelectedItemSafe);
                    return;
                }

                if (_viewModel.SelectedPart == SelectedPartEnum.ToolBar)
                {
                    ActionStop(true);

                    if (_viewModel.SelectedToolbarItemName == "ToolbarItemSettings")
                    {
                        Device.BeginInvokeOnMainThread(() =>
                        {
                            ToolbarItemSettings_Clicked(this, null);
                            _viewModel.SelectedToolbarItemName = null;
                            _viewModel.SelectedPart = SelectedPartEnum.ChannelsList;
                        });
                    }
                    else if (_viewModel.SelectedToolbarItemName == "ToolbarItemInfo")
                    {
                        Device.BeginInvokeOnMainThread(() =>
                        {
                            Detail_Clicked(this, null);
                            _viewModel.SelectedToolbarItemName = null;
                            _viewModel.SelectedPart = SelectedPartEnum.ChannelsList;
                        });
                    }
                    else if (_viewModel.SelectedToolbarItemName == "ToolbarItemQuality")
                    {
                        Device.BeginInvokeOnMainThread(() =>
                        {
                            ToolbarItemQuality_Clicked(this, null);
                            _viewModel.SelectedToolbarItemName = null;
                            _viewModel.SelectedPart = SelectedPartEnum.ChannelsList;
                        });
                    }
                    else if (_viewModel.SelectedToolbarItemName == "ToolbarItemFilter")
                    {
                        Device.BeginInvokeOnMainThread(() =>
                        {
                            ToolbarItemFilter_Clicked(this, null);
                            _viewModel.SelectedToolbarItemName = null;
                            _viewModel.SelectedPart = SelectedPartEnum.ChannelsList;
                        });
                    }
                    else if (_viewModel.SelectedToolbarItemName == "ToolbarItemHelp")
                    {
                        Device.BeginInvokeOnMainThread(() =>
                        {
                            ToolbarItemHelp_Clicked(this, null);
                            _viewModel.SelectedToolbarItemName = null;
                            _viewModel.SelectedPart = SelectedPartEnum.ChannelsList;
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "ActionKeyOK general error");
                //MessagingCenter.Send($"Chyba: {ex.Message}", BaseViewModel.MSG_ToastMessage);
            }
        }

        private async Task ActionKeyLeft()
        {
            _loggingService.Info($"ActionKeyLeft");

            try
            {
                if (PlayingState == PlayingStateEnum.PlayingInternal)
                {
                    _lastSingleClicked = DateTime.MinValue;

                    // play last channel?
                    if (_lastPlayedChannels != null &&
                        _lastPlayedChannels[0] != _viewModel.SelectedItemSafe)
                    {
                        _viewModel.SelectedItemSafe = _lastPlayedChannels[0];
                        await _viewModel.PlaySelectedChannel();
                    }
                    else
                    if (!_viewModel.StandingOnStart)
                    {

                        await _viewModel.SelectPreviousChannel();
                        await _viewModel.PlaySelectedChannel();
                    }
                }
                else
                {
                    if (_viewModel.SelectedPart == SelectedPartEnum.ChannelsList)
                    {
                        _viewModel.SelectedPart = SelectedPartEnum.ToolBar;
                        _viewModel.SelectedToolbarItemName = "ToolbarItemSettings";
                        _viewModel.NotifyToolBarChange();
                    }
                    else if (_viewModel.SelectedPart == SelectedPartEnum.EPGDetail)
                    {
                        await ScrollViewChannelEPGDescription.ScrollToAsync(0, 0, false);
                        _viewModel.SelectedPart = SelectedPartEnum.ChannelsList;
                    }
                    else if (_viewModel.SelectedPart == SelectedPartEnum.ToolBar)
                    {
                        SelecPreviousToolBarItem();
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "ActionKeyLeft general error");
                //MessagingCenter.Send($"Chyba: {ex.Message}", BaseViewModel.MSG_ToastMessage);
            }
        }

        private async Task ActionKeyRight()
        {
            _loggingService.Info($"ActionKeyRight");

            try
            {
                if (PlayingState == PlayingStateEnum.PlayingInternal)
                {
                    if (!_viewModel.StandingOnEnd)
                    {
                        _lastSingleClicked = DateTime.MinValue;
                        await _viewModel.SelectNextChannel();
                        await _viewModel.PlaySelectedChannel();
                    }
                }
                else
                {
                    if (_viewModel.SelectedPart == SelectedPartEnum.ChannelsList)
                    {
                        if (_viewModel.IsPortrait || _viewModel.Channels.Count == 0 || String.IsNullOrEmpty(_viewModel.SelectedChannelEPGDescription))
                        {
                            // no EPG detail on right
                            _viewModel.SelectedPart = SelectedPartEnum.ToolBar;
                            _viewModel.SelectedToolbarItemName = "ToolbarItemHelp";
                            _viewModel.NotifyToolBarChange();
                        }
                        else
                        {
                            // EPG detail on right
                            _viewModel.SelectedPart = SelectedPartEnum.EPGDetail;
                        }
                    }
                    else if (_viewModel.SelectedPart == SelectedPartEnum.ToolBar)
                    {
                        SelectNextToolBarItem(true);
                    }
                    else if (_viewModel.SelectedPart == SelectedPartEnum.EPGDetail)
                    {
                        _viewModel.SelectedPart = SelectedPartEnum.ToolBar;
                        _viewModel.SelectedToolbarItemName = "ToolbarItemHelp";
                        _viewModel.NotifyToolBarChange();
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "ActionKeyRight general error");
                //MessagingCenter.Send($"Chyba: {ex.Message}", BaseViewModel.MSG_ToastMessage);
            }
        }

        private async Task ActionFirstOrLast(bool first)
        {
            _loggingService.Info($"ActionFirstOrLast");

            try
            {
                if (PlayingState != PlayingStateEnum.PlayingInternal)
                {
                    if (_viewModel.SelectedPart == SelectedPartEnum.ChannelsList)
                    {
                        await _viewModel.SelectFirstOrLastChannel(first);
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "ActionFirstOrLast general error");
                //MessagingCenter.Send($"Chyba: {ex.Message}", BaseViewModel.MSG_ToastMessage);
            }
        }

        private async Task ActionKeyDown(int step)
        {
            _loggingService.Info($"ActionKeyDown");

            try
            {
                if (PlayingState == PlayingStateEnum.PlayingInternal)
                {
                    if (!_viewModel.StandingOnEnd)
                    {
                        _lastSingleClicked = DateTime.MinValue;
                        await _viewModel.SelectNextChannel(step);
                        await _viewModel.PlaySelectedChannel();
                    }
                }
                else
                {
                    if (_viewModel.SelectedPart == SelectedPartEnum.ChannelsList)
                    {
                        await _viewModel.SelectNextChannel(step);
                    }
                    else if (_viewModel.SelectedPart == SelectedPartEnum.EPGDetail)
                    {
                        await ScrollViewChannelEPGDescription.ScrollToAsync(ScrollViewChannelEPGDescription.ScrollX, ScrollViewChannelEPGDescription.ScrollY + 10 + (int)_config.AppFontSize, false);
                    }
                    else if (_viewModel.SelectedPart == SelectedPartEnum.ToolBar)
                    {
                        _viewModel.SelectedPart = SelectedPartEnum.ChannelsList;
                        _viewModel.SelectedToolbarItemName = null;
                        _viewModel.NotifyToolBarChange();
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "ActionKeyDown general error");
                //MessagingCenter.Send($"Chyba: {ex.Message}", BaseViewModel.MSG_ToastMessage);
            }
        }

        private async Task ActionKeyUp(int step)
        {
            _loggingService.Info($"ActionKeyUp");

            try
            {
                if (PlayingState == PlayingStateEnum.PlayingInternal)
                {
                    if (!_viewModel.StandingOnStart)
                    {
                        _lastSingleClicked = DateTime.MinValue;
                        await _viewModel.SelectPreviousChannel(step);
                        await _viewModel.PlaySelectedChannel();
                    }
                }
                else
                {
                    if (_viewModel.SelectedPart == SelectedPartEnum.ChannelsList)
                    {
                        await _viewModel.SelectPreviousChannel(step);
                    }
                    else if (_viewModel.SelectedPart == SelectedPartEnum.EPGDetail)
                    {
                        await ScrollViewChannelEPGDescription.ScrollToAsync(ScrollViewChannelEPGDescription.ScrollX, ScrollViewChannelEPGDescription.ScrollY - (10 + (int)_config.AppFontSize), false);
                    }
                    else if (_viewModel.SelectedPart == SelectedPartEnum.ToolBar)
                    {
                        SelectNextToolBarItem(false);
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "ActionKeyUp general error");
                //MessagingCenter.Send($"Chyba: {ex.Message}", BaseViewModel.MSG_ToastMessage);
            }
        }

        private void SelecPreviousToolBarItem()
        {
            _loggingService.Info($"SelecPreviousToolBarItem");

            if (_viewModel.SelectedToolbarItemName == null)
            {
                _viewModel.SelectedToolbarItemName = "ToolbarItemSettings";
            }
            else
            if (_viewModel.SelectedToolbarItemName == "ToolbarItemSettings")
            {
                _viewModel.SelectedToolbarItemName = "ToolbarItemInfo";
            }
            else
            if (_viewModel.SelectedToolbarItemName == "ToolbarItemInfo")
            {
                if (_viewModel.QualityFilterEnabled)
                {
                    _viewModel.SelectedToolbarItemName = "ToolbarItemQuality";
                } else
                {
                    _viewModel.SelectedToolbarItemName = "ToolbarItemFilter";
                }
            }
            else
            if (_viewModel.SelectedToolbarItemName == "ToolbarItemQuality")
            {
                _viewModel.SelectedToolbarItemName = "ToolbarItemFilter";
            } else
            if (_viewModel.SelectedToolbarItemName == "ToolbarItemFilter")
            {
                _viewModel.SelectedToolbarItemName = "ToolbarItemHelp";
            }
            else
            if (_viewModel.SelectedToolbarItemName == "ToolbarItemHelp")
            {
                _viewModel.SelectedPart = SelectedPartEnum.ChannelsList;
                _viewModel.SelectedToolbarItemName = null;
            }

            _viewModel.NotifyToolBarChange();
        }

        private void SelectNextToolBarItem(bool canExitToolBar)
        {
            _loggingService.Info($"SelectNextToolBarItem");

            if (_viewModel.SelectedToolbarItemName == null)
            {
                _viewModel.SelectedToolbarItemName = "ToolbarItemSettings";
            } else
            if (_viewModel.SelectedToolbarItemName == "ToolbarItemSettings")
            {
                if (canExitToolBar)
                {
                    _viewModel.SelectedPart = SelectedPartEnum.ChannelsList;
                    _viewModel.SelectedToolbarItemName = null;
                } else
                {
                    _viewModel.SelectedToolbarItemName = "ToolbarItemHelp";
                }
            } else
            if (_viewModel.SelectedToolbarItemName == "ToolbarItemHelp")
            {
                _viewModel.SelectedToolbarItemName = "ToolbarItemFilter";
            }
            else
            if (_viewModel.SelectedToolbarItemName == "ToolbarItemFilter")
            {
                if (_viewModel.QualityFilterEnabled)
                {
                    _viewModel.SelectedToolbarItemName = "ToolbarItemQuality";
                } else
                {
                    _viewModel.SelectedToolbarItemName = "ToolbarItemInfo";
                }
            } else
            if (_viewModel.SelectedToolbarItemName == "ToolbarItemQuality")
            {
                _viewModel.SelectedToolbarItemName = "ToolbarItemInfo";
            } else
            if (_viewModel.SelectedToolbarItemName == "ToolbarItemInfo")
            {
                _viewModel.SelectedToolbarItemName = "ToolbarItemSettings";
            }

            _viewModel.NotifyToolBarChange();
        }

        private bool IsPageOnTop(Type type)
        {
            var stack = Navigation.NavigationStack;
            var typeOfPageOnTop = stack[stack.Count - 1].GetType();

            if (typeOfPageOnTop == type)
                return true;

            return false;
        }

        private async void ToolbarItemHelp_Clicked(object sender, EventArgs e)
        {
            _loggingService.Info($"ToolbarItemHelp_Clicked");

            if (IsPageOnTop(typeof(HelpPage)))
                return;

            var helpPage = new HelpPage(_loggingService, _config, _dialogService, _viewModel.TVService);
            await Navigation.PushAsync(helpPage);
        }

        private async void ToolbarItemFilter_Clicked(object sender, EventArgs e)
        {
            _loggingService.Info($"ToolbarItemFilter_Clicked");

            if (IsPageOnTop(typeof(FilterPage)))
                return;

            _filterPage = new FilterPage(_loggingService, _config, _viewModel.TVService);
            _filterPage.Disappearing += delegate
            {
                _viewModel.RefreshCommandWithNotification.Execute(null);
            };

            await Navigation.PushAsync(_filterPage);
        }

        private async void ToolbarItemSettings_Clicked(object sender, EventArgs e)
        {
            _loggingService.Info($"ToolbarItemSettings_Clicked");

            if (IsPageOnTop(typeof(SettingsPage)))
                return;

            if (_settingsPage == null)
            {
                _settingsPage = new SettingsPage(_loggingService, _config, _dialogService, _viewModel.TVService);
                _settingsPage.FillAutoPlayChannels(_viewModel.AllNotFilteredChannels);
                _settingsPage.AppVersion = AppVersion;

                _settingsPage.Disappearing += delegate
                {
                    _viewModel.ResetConnectionCommand.Execute(null);
                    _viewModel.RefreshCommandWithNotification.Execute(null);

                    RestartRemoteAccessService();
                };
            }

            await Navigation.PushAsync(_settingsPage);
        }

        private void RestartRemoteAccessService()
        {
            if (_config.AllowRemoteAccessService)
            {
                if (_remoteAccessService.IsBusy)
                {
                    if (_remoteAccessService.ParamsChanged(_config.RemoteAccessServiceIP, _config.RemoteAccessServicePort, _config.RemoteAccessServiceSecurityKey))
                    {
                        _remoteAccessService.StopListening();
                        _remoteAccessService.SetConnection(_config.RemoteAccessServiceIP, _config.RemoteAccessServicePort, _config.RemoteAccessServiceSecurityKey);
                        _remoteAccessService.StartListening(OnMessageReceived, BaseViewModel.DeviceFriendlyName);
                    }
                }
                else
                {
                    _remoteAccessService.SetConnection(_config.RemoteAccessServiceIP, _config.RemoteAccessServicePort, _config.RemoteAccessServiceSecurityKey);
                    _remoteAccessService.StartListening(OnMessageReceived, BaseViewModel.DeviceFriendlyName);
                }
            }
            else
            {
                _remoteAccessService.StopListening();
            }
        }

        private async void ToolbarItemQuality_Clicked(object sender, EventArgs e)
        {
            _loggingService.Info($"ToolbarItemQuality_Clicked");

            if (IsPageOnTop(typeof(QualitiesPage)))
                return;

            var qualitiesPage = new QualitiesPage(_loggingService, _config, _viewModel.TVService);

            qualitiesPage.Disappearing += delegate
            {
                _viewModel.RefreshCommandWithNotification.Execute(null);
            };

            await Navigation.PushAsync(qualitiesPage);
        }

        private async void Detail_Clicked(object sender, EventArgs e)
        {
           _loggingService.Info($"Detail_Clicked");

            // this event is called immediately after Navigation.PopAsync();
            if (_lastDetailclickedTime != DateTime.MinValue && ((DateTime.Now - _lastDetailclickedTime).TotalSeconds < 1))
            {
                // ignoring this event
                return;
            }

            _lastDetailclickedTime = DateTime.Now;

            if (PlayingState == PlayingStateEnum.PlayingInternal)
            {
                ShowJustPlayingNotification();
            }
            else
            {
                await _viewModel.ShowPopupMenu(_viewModel.SelectedItemSafe);
            }
        }

        private void ActionBack(bool longPress)
        {
            _loggingService.Info($"OnBackButtonPressed");

            if (PlayingState == PlayingStateEnum.PlayingInternal || PlayingState == PlayingStateEnum.PlayingInPreview)
            {
                ActionStop(longPress);
                _lastBackPressedTime = DateTime.MinValue;
                return;
            }

            if ((_lastBackPressedTime == DateTime.MinValue) || ((DateTime.Now-_lastBackPressedTime).TotalSeconds>3))
            {
                if (longPress)
                {
                    MessagingCenter.Send<string>(string.Empty, BaseViewModel.MSG_StopPlayInternalNotificationAndQuit);
                }

                MessagingCenter.Send($"Stiskněte ještě jednou pro ukončení", BaseViewModel.MSG_ToastMessage);
                _lastBackPressedTime = DateTime.Now;
            } else
            {
                MessagingCenter.Send<string>(string.Empty, BaseViewModel.MSG_StopPlayInternalNotificationAndQuit);
            }
        }

        private async Task CheckStream()
        {
            _loggingService.Debug("CheckStream");

            Device.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    AudioPlayingImage.IsVisible = false;

                    if (PlayingState == PlayingStateEnum.Stopped)
                    {
                        NoVideoStackLayout.IsVisible = false;
                        VideoStackLayout.IsVisible = false;

                        return;
                    }

                    if (!_mediaPlayer.IsPlaying)
                    {
                        _mediaPlayer.Play(_media);
                    }

                    var radio = _viewModel.PlayingChannel != null
                                && !string.IsNullOrEmpty(_viewModel.PlayingChannel.Type)
                                && (_viewModel.PlayingChannel.Type.ToLower() == "radio")
                                ? true
                                : false;
                    if ((_mediaPlayer.VideoTrackCount == 0) || radio)
                    {
                        NoVideoStackLayout.IsVisible = true;
                        VideoStackLayout.IsVisible = false;
                        AudioPlayingImage.IsVisible = true;
                    }
                    else
                    {
                        PreviewVideoBordersFix();

                        NoVideoStackLayout.IsVisible = false;
                        VideoStackLayout.IsVisible = true;
                    }

                    if (_mediaPlayer.IsPlaying &&
                        _viewModel.PlayingChannel != null)
                    {

                        // there can be more video tracks, but MediaPlayer always returns video track id -1
                        // ==> detecting video with biggest height

                        var height = (uint)0;

                        foreach (var track in _mediaPlayer.Media.Tracks)
                        {
                            if (track.TrackType == TrackType.Video && track.Data.Video.Height > height)
                            {
                                _viewModel.PlayingChannel.VideoTrackDescription = $"{track.Data.Video.Width}x{track.Data.Video.Height}   ";
                                height = track.Data.Video.Height;
                            }
                        }

                        if (_viewModel.PlayingChannel.AudioTracks != null &&
                            _viewModel.PlayingChannel.AudioTracks.Count == 0)
                        {
                            _viewModel.PlayingChannel.AudioTracks = GetAudioTracks();
                        }


                        foreach (var desc in _mediaPlayer.SpuDescription)
                        {
                            if (desc.Id >= 0)
                            {

                            }
                        }

                    }

                } catch (Exception ex)
                {
                    _loggingService.Error(ex, "CheckStream error");
                }
            });
        }

        private void PreviewVideoBordersFix()
        {
            try
            {
                if (PlayingState == PlayingStateEnum.PlayingInPreview && _viewModel.IsPortrait &&
                    _media.Tracks != null &&
                    _media.Tracks.Length > 0 &&
                    _mediaPlayer.VideoTrackCount > 0 &&
                    _mediaPlayer.VideoTrack != -1
                    )
                {
                    var originalVideoWidth = _media.Tracks[0].Data.Video.Width;
                    var originalVideoHeight = _media.Tracks[0].Data.Video.Height;

                    if (originalVideoWidth == 0 || originalVideoHeight == 0)
                        return; // video not initialized yet?

                    var aspect = (double)originalVideoWidth / (double)originalVideoHeight;
                    var newVideoHeight = VideoStackLayout.Width / aspect;

                    var borderHeight = (VideoStackLayout.Height - newVideoHeight) / 2.0;

                    var rect = new Rectangle()
                    {
                        Left = VideoStackLayout.X,
                        Top = VideoStackLayout.Y + borderHeight,
                        Width = VideoStackLayout.Width,
                        Height = newVideoHeight
                    };

                    if (rect.X != VideoStackLayout .X ||
                        rect.Y != VideoStackLayout.Y ||
                        rect.Width != VideoStackLayout.Width ||
                        rect.Height != VideoStackLayout.Height)
                    {
                        AbsoluteLayout.SetLayoutFlags(VideoStackLayout, AbsoluteLayoutFlags.None);
                        AbsoluteLayout.SetLayoutBounds(VideoStackLayout, rect);

                        //VideoStackLayout.Layout(rect);
                    }
                }
            } catch (Exception ex)
            {
                _loggingService.Error(ex);
            }
        }

        private void ChannelsListView_ItemTapped(object sender, ItemTappedEventArgs e)
        {
            if (Device.OS == TargetPlatform.Windows)
            {
                _viewModel.ShortPressCommand.Execute(_viewModel.SelectedItemSafe);
            }
        }
    }
}
