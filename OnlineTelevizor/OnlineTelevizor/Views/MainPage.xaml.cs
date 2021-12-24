using LoggerService;
using SledovaniTVAPI;
using OnlineTelevizor.Models;
using OnlineTelevizor.Services;
using OnlineTelevizor.ViewModels;
using OnlineTelevizor.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using System.Threading;
using static OnlineTelevizor.ViewModels.MainPageViewModel;
using static Android.OS.PowerManager;
using LibVLCSharp.Shared;

namespace OnlineTelevizor.Views
{
    public partial class MainPage : ContentPage
    {
        private MainPageViewModel _viewModel;
        private DialogService _dialogService;
        private IOnlineTelevizorConfiguration _config;
        private ILoggingService _loggingService;

        private FilterPage _filterPage = null;
        private CastRenderersPage _renderersPage;

        private DateTime _lastNumPressedTime = DateTime.MinValue;
        private DateTime _lastBackPressedTime = DateTime.MinValue;
        private DateTime _lastKeyLongPressedTime = DateTime.MinValue;
        private DateTime _lastToggledAudioStreamTime = DateTime.MinValue;
        private DateTime _lastPageAppearedTime = DateTime.MinValue;
        private bool _firstSelectionAfterStartup = false;
        private string _numberPressed = String.Empty;

        private LibVLC _libVLC = null;
        private MediaPlayer _mediaPlayer;
        private Media _media = null;

        private PlayingStateEnum _playingState = PlayingStateEnum.Stopped;
        private Size _lastAllocatedSize = new Size(-1, -1);

        private DateTime _lastSingleClicked = DateTime.MinValue;

        public Command CheckStreamCommand { get; set; }

        public enum PlayingStateEnum
        {
            Stopped = 0,
            PlayingInternal = 1,
            PlayingInPreview = 2,
            PlayingExternal = 3
        }

        public MainPage(ILoggingService loggingService, IOnlineTelevizorConfiguration config)
        {
            InitializeComponent();

            _dialogService = new DialogService(this);

            _config = config;
            _loggingService = loggingService;

            Core.Initialize();

            _libVLC = new LibVLC();
            _mediaPlayer = new MediaPlayer(_libVLC) { EnableHardwareDecoding = true };
            videoView.MediaPlayer = _mediaPlayer;

            _loggingService.Debug($"Initializing MainPage");

            BindingContext = _viewModel = new MainPageViewModel(loggingService, config, _dialogService);

            ScrollViewChannelEPGDescription.Scrolled += ScrollViewChannelEPGDescription_Scrolled;
            Appearing += MainPage_Appearing;
            Disappearing += MainPage_Disappearing;

            ChannelsListView.ItemSelected += ChannelsListView_ItemSelected;
            ChannelsListView.Scrolled += ChannelsListView_Scrolled;

            PlayingState = PlayingStateEnum.Stopped;

            CheckStreamCommand = new Command(async () => await CheckStream());

            BackgroundCommandWorker.RunInBackground(CheckStreamCommand, 3, 2);

            MessagingCenter.Subscribe<string>(this, BaseViewModel.KeyMessage, (key) =>
            {
                OnKeyDown(key);
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.KeyLongMessage, (key) =>
            {
                _lastKeyLongPressedTime = DateTime.Now;
            });

            MessagingCenter.Subscribe<MainPageViewModel>(this, BaseViewModel.ShowDetailMessage, (sender) =>
            {
                var detailPage = new ChannelDetailPage(_loggingService, _config, _dialogService, _viewModel.TVService);
                detailPage.Channel = _viewModel.SelectedItem;

                Navigation.PushAsync(detailPage);
            });

            MessagingCenter.Subscribe<MainPageViewModel>(this, BaseViewModel.ShowRenderers, (sender) =>
            {
                if (_viewModel.SelectedItem == null)
                    return;

                if (_renderersPage == null)
                {
                    _renderersPage = new CastRenderersPage(_loggingService, _config);
                }

                _renderersPage.Channel = _viewModel.SelectedItem;

                Navigation.PushAsync(_renderersPage);
            });

            MessagingCenter.Subscribe<MainPageViewModel>(this, BaseViewModel.StopCasting, (sender) =>
            {
                if (_renderersPage != null)
                {
                    _renderersPage.StopCasting();
                }
            });

            MessagingCenter.Subscribe<BaseViewModel, ChannelItem>(this, BaseViewModel.PlayInternal, (sender, channel) =>
            {
                ActionPlay(channel);
            });

            MessagingCenter.Subscribe<MainPageViewModel>(this, BaseViewModel.ShowConfiguration, (sender) =>
            {
                ToolbarItemSettings_Clicked(this, null);
            });

            MessagingCenter.Subscribe<BaseViewModel,ChannelItem>(this, BaseViewModel.CastingStarted, (sender, channel) =>
            {
                Task.Run(async () =>
                {
                    await _viewModel.NotifyCastChannel(channel.ChannelNumber, true);
                });
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.CastingStopped, (channelNumber) =>
            {
                Task.Run(async () =>
                {
                    await _viewModel.NotifyCastChannel(channelNumber, false);
                });
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.StopPlay, async (sender) =>
            {
                ActionStop(false);
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.StopRecord, async (sender) =>
            {
                await Task.Run(async () =>
                {
                    await _viewModel.RecordChannel(false);
                });
            });
        }

        public void RefreshGUI()
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                switch (_playingState)
                {
                    case PlayingStateEnum.PlayingInternal:

                        // turn off tool bar
                        NavigationPage.SetHasNavigationBar(this, false);

                        MessagingCenter.Send(String.Empty, BaseViewModel.EnableFullScreen);

                        LayoutGrid.ColumnDefinitions[0].Width = new GridLength(0, GridUnitType.Absolute);
                        LayoutGrid.ColumnDefinitions[1].Width = new GridLength(100, GridUnitType.Star);

                        StackLayoutEPGDetail.RowDefinitions[2].Height = new GridLength(40, GridUnitType.Star);
                        StackLayoutEPGDetail.RowDefinitions[3].Height = new GridLength(40, GridUnitType.Star);

                        // VideoStackLayout must be visible before changing Layout
                        var isVideoStackLayoutVisible = VideoStackLayout.IsVisible;
                        VideoStackLayout.IsVisible = true;
                        AbsoluteLayout.SetLayoutFlags(VideoStackLayout, AbsoluteLayoutFlags.All);
                        AbsoluteLayout.SetLayoutBounds(VideoStackLayout, new Rectangle(0, 0, 1, 1));
                        VideoStackLayout.IsVisible = isVideoStackLayoutVisible;

                        AbsoluteLayout.SetLayoutFlags(NoVideoStackLayout, AbsoluteLayoutFlags.All);
                        AbsoluteLayout.SetLayoutBounds(NoVideoStackLayout, new Rectangle(0, 1, 1, 0.3));

                        break;
                    case PlayingStateEnum.PlayingInPreview:

                        NavigationPage.SetHasNavigationBar(this, true);

                        if (!_config.Fullscreen)
                        {
                            MessagingCenter.Send(String.Empty, BaseViewModel.DisableFullScreen);
                        }

                        if (_viewModel.IsPortrait)
                        {
                            LayoutGrid.ColumnDefinitions[0].Width = new GridLength(100, GridUnitType.Star);
                            LayoutGrid.ColumnDefinitions[1].Width = new GridLength(0, GridUnitType.Absolute);

                            StackLayoutEPGDetail.RowDefinitions[2].Height = new GridLength(80, GridUnitType.Star);
                            StackLayoutEPGDetail.RowDefinitions[3].Height = new GridLength(0, GridUnitType.Absolute);
                        }
                        else
                        {
                            LayoutGrid.ColumnDefinitions[0].Width = new GridLength(50, GridUnitType.Star);
                            LayoutGrid.ColumnDefinitions[1].Width = new GridLength(50, GridUnitType.Star);

                            StackLayoutEPGDetail.RowDefinitions[2].Height = new GridLength(40, GridUnitType.Star);
                            StackLayoutEPGDetail.RowDefinitions[3].Height = new GridLength(40, GridUnitType.Star);
                        }

                        AbsoluteLayout.SetLayoutFlags(VideoStackLayout, AbsoluteLayoutFlags.All);
                        AbsoluteLayout.SetLayoutBounds(VideoStackLayout, new Rectangle(1, 1, 0.5, 0.3));

                        AbsoluteLayout.SetLayoutFlags(NoVideoStackLayout, AbsoluteLayoutFlags.All);
                        AbsoluteLayout.SetLayoutBounds(NoVideoStackLayout, new Rectangle(1, 1, 0.5, 0.3));

                        CheckStreamCommand.Execute(null);

                        break;
                    case PlayingStateEnum.Stopped:

                        NavigationPage.SetHasNavigationBar(this, true);

                        if (!_config.Fullscreen)
                        {
                            MessagingCenter.Send(String.Empty, BaseViewModel.DisableFullScreen);
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

                        StackLayoutEPGDetail.RowDefinitions[2].Height = new GridLength(80, GridUnitType.Star);
                        StackLayoutEPGDetail.RowDefinitions[3].Height = new GridLength(0, GridUnitType.Absolute);

                        VideoStackLayout.IsVisible = false;
                        NoVideoStackLayout.IsVisible = false;

                        break;
                }
            });
        }

        private void MainPage_Disappearing(object sender, EventArgs e)
        {

        }

        private void SwipeGestureRecognizer_Swiped(object sender, SwipedEventArgs e)
        {
            ActionStop(false);
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

        private void OnSingleTapped(object sender, EventArgs e)
        {
            ActionTap(1);
        }

        public void OnDoubleTapped(object sender, EventArgs e)
        {
            ActionTap(2);
        }

        private async void MainPage_Appearing(object sender, EventArgs e)
        {
            _lastPageAppearedTime = DateTime.Now;

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
                    ToolbarItems.Insert(1, ToolbarItemQuality);
                }
            }
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
            System.Diagnostics.Debug.WriteLine($"OnSizeAllocated: {width}/{height}");
            //System.Diagnostics.Debug.WriteLine($"VideoStack Size: {VideoStackLayout.Width}/{VideoStackLayout.Height}");

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
            // workaround for de-highlighting selected item after scroll on startup
            if (_firstSelectionAfterStartup)
            {
                _viewModel.DoNotScrollToChannel = true;
                var item = _viewModel.SelectedItem;
                _viewModel.SelectedItem = null;
                _viewModel.SelectedItem = item;
                _firstSelectionAfterStartup = false;
            }
        }

        private void ChannelsListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (!_viewModel.DoNotScrollToChannel)
            {
                ChannelsListView.ScrollTo(_viewModel.SelectedItem, ScrollToPosition.MakeVisible, false);
                _firstSelectionAfterStartup = true;
            }

            _viewModel.DoNotScrollToChannel = false;
        }

        private static bool LeavePageKeyDown(string lowKey)
        {
            if (lowKey == "escape" ||
                    lowKey == "back" ||
                    lowKey == "numpadsubtract" ||
                    lowKey == "f4" ||
                    lowKey == "mediaplaystop" ||
                    lowKey == "mediastop" ||
                    lowKey == "pageup" ||
                    lowKey == "a" ||
                    lowKey == "b" ||
                    lowKey == "mediaplayprevious" ||
                    lowKey == "mediaprevious" ||
                    lowKey == "numpad4" ||
                    lowKey == "left" ||
                    lowKey == "dpadleft" ||
                    lowKey == "dpadup" ||
                    lowKey == "buttonl1" ||
                    lowKey == "up" ||
                    lowKey == "w" ||
                    lowKey == "numpad8" ||
                    lowKey == "f3"
                )
            {
                return true;
            }

            return false;
        }

        private static bool SelectNextItemKeyDown(string lowKey)
        {
            if (lowKey == "right" ||
                lowKey == "dpadright" ||
                lowKey == "mediaplaynext" ||
                lowKey == "medianext" ||
                lowKey == "dpaddown" ||
                lowKey == "buttonr1" ||
                lowKey == "down" ||
                lowKey == "s" ||
                lowKey == "numpad2" ||
                lowKey == "f2"
                )
            {
                return true;
            }

            return false;
        }

        public void OnKeyDown(string key)
        {
            _loggingService.Debug($"OnKeyDown {key}");
            var lowKey = key.ToLower();

            // key events can be consumed only on this MainPage

            var stack = Navigation.NavigationStack;
            if  (stack[stack.Count - 1].GetType() != typeof(MainPage))
            {
                // different page on navigation top

                if (stack[stack.Count - 1].GetType() == typeof(ChannelDetailPage))
                {
                    if (LeavePageKeyDown(lowKey))
                    {
                        // closing detail page
                        Navigation.PopAsync();
                    }
                }

                if (stack[stack.Count - 1].GetType() == typeof(FilterPage))
                {
                    if (LeavePageKeyDown(lowKey))
                    {
                        // closing filter page
                        Navigation.PopAsync();
                    }

                    if (_filterPage != null)
                    {
                        if (SelectNextItemKeyDown(lowKey))
                            _filterPage.SelectNextItem();
                    }
                }

                if (stack[stack.Count - 1].GetType() == typeof(QualitiesPage))
                {
                    if (LeavePageKeyDown(lowKey))
                    {
                        // closing quality page
                        Navigation.PopAsync();
                    }

                    var qualityPage = stack[stack.Count - 1] as QualitiesPage;

                    if (SelectNextItemKeyDown(lowKey))
                        qualityPage.SelectNextItem();
                }

                if (stack[stack.Count - 1].GetType() == typeof(SettingsPage))
                {
                    if (LeavePageKeyDown(lowKey))
                    {
                        // closing settings page
                        Navigation.PopAsync();
                    }

                    var settingsPage = stack[stack.Count - 1] as SettingsPage;

                    if (SelectNextItemKeyDown(lowKey))
                        settingsPage.SelectNextItem();
                }

                return;
            }

            switch (lowKey)
            {
                case "dpaddown":
                case "buttonr1":
                case "down":
                case "s":
                case "numpad2":
                case "f3":
                case "mediaplaynext":
                case "medianext":
                case "moveend":
                    Task.Run(async () => await ActionKeyDown(1));
                    break;
                case "dpadup":
                case "buttonl1":
                case "up":
                case "w":
                case "numpad8":
                case "f2":
                case "mediaplayprevious":
                case "mediaprevious":
                case "movehome":
                    Task.Run(async () => await ActionKeyUp(1));
                    break;
                case "pagedown":
                    Task.Run(async () => await ActionKeyDown(10));
                    break;
                case "pageup":
                    Task.Run(async () => await ActionKeyUp(10));
                    break;
                case "dpadleft":
                case "left":
                case "a":
                case "b":
                case "numpad4":
                case "leftbracket":
                    Task.Run(async () => await ActionKeyLeft());
                    break;
                case "dpadright":
                case "right":
                case "d":
                case "f":
                case "numpad6":
                case "rightbracket":
                    Task.Run(async () => await ActionKeyRight());
                    break;
                case "f6":
                case "dpadcenter":
                case "space":
                case "buttonr2":
                case "mediaplay":
                case "mediaplaypause":
                case "enter":
                case "numpad5":
                case "numpadenter":
                case "buttona":
                case "buttonstart":
                case "capslock":
                case "comma":
                case "semicolon":
                case "grave":
                    Task.Run(async () => await ActionKeyOK());
                    break;
                //case "back":
                case "f4":
                case "f7":
                case "escape":
                case "mediaplaystop":
                case "mediapause":
                case "mediaclose":
                case "mediastop":
                case "numpadsubtract":
                case "del":
                case "forwarddel":
                case "delete":
                case "buttonx":
                case "altleft":
                case "minus":
                case "period":
                case "apostrophe":
                case "buttonselect":
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
                    Reset();
                    Refresh();
                    break;
                default:
                    {
#if DEBUG
    MessagingCenter.Send($"Unbound key down: {key}", BaseViewModel.ToastMessage);
#endif
                    }
                    break;
            }
        }

        private void ToggleAudioStream()
        {
            if (_mediaPlayer == null)
                return;

            if (_mediaPlayer.AudioTrackCount <= 1)
                return;

            var currentAudioTrack = _mediaPlayer.AudioTrack;
            if (currentAudioTrack == -1)
                return;

            var select = false;
            var selected = false;

            var firstAudioTrackId = -1;
            string firstAudioTrackName = null;

            string selectedName = null;
            int selectedId = -1;

            foreach (var desc in _mediaPlayer.AudioTrackDescription)
            {
                if (firstAudioTrackId == -1 && desc.Id != -1)
                {
                    firstAudioTrackId = desc.Id;
                    firstAudioTrackName = desc.Name;
                }

                if (desc.Id ==currentAudioTrack)
                {
                    select = true;
                } else
                {
                    if (select)
                    {
                        _mediaPlayer.SetAudioTrack(desc.Id);
                        selectedName = desc.Name;
                        selectedId = desc.Id;
                        selected = true;
                        break;
                    }
                }
            }

            if (!selected)
            {
                _mediaPlayer.SetAudioTrack(firstAudioTrackId);

                selectedName = firstAudioTrackName;
                selectedId = firstAudioTrackId;
            }

            if (string.IsNullOrEmpty(selectedName)) selectedName = $"# {selectedId}";

            MessagingCenter.Send($"Zvolena zvuková stopa {selectedName}", BaseViewModel.ToastMessage);
        }

        private void HandleNumKey(int number)
        {
            _loggingService.Debug($"HandleNumKey {number}");

            if ((DateTime.Now - _lastNumPressedTime).TotalSeconds > 1)
            {
                _lastNumPressedTime = DateTime.MinValue;
                _numberPressed = String.Empty;
            }

            _lastNumPressedTime = DateTime.Now;
            _numberPressed += number;

            MessagingCenter.Send(_numberPressed, BaseViewModel.ToastMessage);

            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                var numberPressedBefore = _numberPressed;

                Thread.Sleep(2000);

                if (numberPressedBefore == _numberPressed)
                {
                    Task.Run(async () =>
                    {
                        await _viewModel.SelectChannelByNumber(_numberPressed);

                        if (
                                (_viewModel.SelectedItem != null) &&
                                (_numberPressed == _viewModel.SelectedItem.ChannelNumber)
                           )
                        {
                            await _viewModel.Play(_viewModel.SelectedItem);
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
                return _playingState;
            }
            set
            {
                _playingState = value;

                RefreshGUI();
            }
        }

        public void Resume()
        {
            if (_config.Fullscreen)
            {
                MessagingCenter.Send(String.Empty, BaseViewModel.EnableFullScreen);
            }

            // workaround for black screen after resume
            // TODO: resume video without reinitializing

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
            _loggingService.Info($"Refresh");

            _viewModel.RefreshCommand.Execute(null);
        }

        public void ShowJustPlayingNotification()
        {
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
                    msg = $"\u25B6  {_viewModel.PlayingChannel.Name}";
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

            MessagingCenter.Send(msg, BaseViewModel.ToastMessage);
        }

        public void ActionPlay(ChannelItem channel)
        {
            if (_config.InternalPlayer)
            {
                Device.BeginInvokeOnMainThread(() =>
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

                    _mediaPlayer.Play(_media);

                    _viewModel.PlayingChannel = channel;

                    ShowJustPlayingNotification();

                    PlayingState = PlayingStateEnum.PlayingInternal;
                });
            }
        }

        public void ActionTap(int count)
        {
            if (count == 2 && PlayingState == PlayingStateEnum.PlayingInPreview)
            {
                PlayingState = PlayingStateEnum.PlayingInternal;
            }
            else
            if (count == 2 && PlayingState == PlayingStateEnum.PlayingInternal)
            {
                PlayingState = PlayingStateEnum.PlayingInPreview;
            }
            else
            if (count == 1 &&
                (PlayingState == PlayingStateEnum.PlayingInternal || PlayingState == PlayingStateEnum.PlayingInPreview))
            {
                ShowJustPlayingNotification();
            }
        }

        public void ActionStop(bool force)
        {
            if (_config.InternalPlayer)
            {
                Device.BeginInvokeOnMainThread(() =>
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
                    }
                });
            }
            else
            {
                PlayingState = PlayingStateEnum.Stopped;
            }
        }

        private async Task ActionKeyOK()
        {
            if (PlayingState == PlayingStateEnum.PlayingInternal)
            {
                if (LastKeyLongPressed)
                {
                    if ((_lastToggledAudioStreamTime == DateTime.MinValue) || (DateTime.Now - _lastToggledAudioStreamTime).TotalSeconds > 3)
                    {
                        _lastToggledAudioStreamTime = DateTime.Now;

                        ToggleAudioStream();
                    }
                }
                else
                {
                    ShowJustPlayingNotification();
                }
                return;
            }

            if (_viewModel.SelectedPart == SelectedPartEnum.ChannelsList)
            {
                ActionPlay(_viewModel.SelectedItem);
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
            }
        }

        private async Task ActionKeyLeft()
        {
            if (PlayingState == PlayingStateEnum.PlayingInternal)
            {
                await _viewModel.SelectPreviousChannel();
                await _viewModel.PlaySelectedChannel();
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

        private async Task ActionKeyRight()
        {
            if (PlayingState == PlayingStateEnum.PlayingInternal)
            {
                await _viewModel.SelectNextChannel();
                await _viewModel.PlaySelectedChannel();
            }
            else
            {
                if (_viewModel.SelectedPart == SelectedPartEnum.ChannelsList)
                {
                    if (_viewModel.IsPortrait || _viewModel.Channels.Count == 0 || String.IsNullOrEmpty(_viewModel.SelectedChannelEPGDescription))
                    {
                        // no EPG detail on right
                        _viewModel.SelectedPart = SelectedPartEnum.ToolBar;
                        _viewModel.SelectedToolbarItemName = "ToolbarItemFilter";
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
                    _viewModel.SelectedToolbarItemName = "ToolbarItemFilter";
                    _viewModel.NotifyToolBarChange();
                }
            }
        }

        private async Task ActionKeyDown(int step)
        {
            if (PlayingState == PlayingStateEnum.PlayingInternal)
            {
                await _viewModel.SelectNextChannel(step);
                await _viewModel.PlaySelectedChannel();
            }
            else
            {
                if (_viewModel.SelectedPart == SelectedPartEnum.ChannelsList)
                {
                    await _viewModel.SelectNextChannel(step);
                } else if (_viewModel.SelectedPart == SelectedPartEnum.EPGDetail)
                {
                    await ScrollViewChannelEPGDescription.ScrollToAsync(ScrollViewChannelEPGDescription.ScrollX, ScrollViewChannelEPGDescription.ScrollY + 10+(int)_config.AppFontSize, false);
                }
                else if (_viewModel.SelectedPart == SelectedPartEnum.ToolBar)
                {
                    _viewModel.SelectedPart = SelectedPartEnum.ChannelsList;
                    _viewModel.SelectedToolbarItemName = null;
                    _viewModel.NotifyToolBarChange();
                }
            }
        }

        private async Task ActionKeyUp(int step)
        {
            if (PlayingState == PlayingStateEnum.PlayingInternal)
            {
                await _viewModel.SelectPreviousChannel(step);
                await _viewModel.PlaySelectedChannel();
            }
            else
            {
                if (_viewModel.SelectedPart == SelectedPartEnum.ChannelsList)
                {
                    if (_viewModel.StandingOnStart)
                    {
                        _viewModel.SelectedPart = SelectedPartEnum.ToolBar;
                        SelectNextToolBarItem(true);
                    }
                    else
                    {
                        await _viewModel.SelectPreviousChannel(step);
                    }
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

        private void SelecPreviousToolBarItem()
        {
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
            }
            else
            if (_viewModel.SelectedToolbarItemName == "ToolbarItemFilter")
            {
                _viewModel.SelectedPart = SelectedPartEnum.ChannelsList;
                _viewModel.SelectedToolbarItemName = null;
            }

            _viewModel.NotifyToolBarChange();
        }

        private void SelectNextToolBarItem(bool canExitToolBar)
        {
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
                    _viewModel.SelectedToolbarItemName = "ToolbarItemFilter";
                }
            } else
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

        private async void ToolbarItemFilter_Clicked(object sender, EventArgs e)
        {
            _loggingService.Info($"ToolbarItemFilter_Clicked");

            _filterPage = new FilterPage(_loggingService, _config, _viewModel.TVService);
            _filterPage.Disappearing += delegate
            {
                _viewModel.RefreshCommand.Execute(null);
            };

            await Navigation.PushAsync(_filterPage);
        }

        private async void ToolbarItemSettings_Clicked(object sender, EventArgs e)
        {
            _loggingService.Info($"ToolbarItemSettings_Clicked");

            var settingsPage = new SettingsPage(_loggingService, _config, _dialogService, _viewModel.TVService);
            settingsPage.FillAutoPlayChannels(_viewModel.AllNotFilteredChannels);

            settingsPage.Disappearing += delegate
            {
                _viewModel.ResetConnectionCommand.Execute(null);
                _viewModel.RefreshCommand.Execute(null);
            };

            await Navigation.PushAsync(settingsPage);
        }

        private async void ToolbarItemQuality_Clicked(object sender, EventArgs e)
        {
            _loggingService.Info($"ToolbarItemQuality_Clicked");

            var qualitiesPage = new QualitiesPage(_loggingService, _config, _viewModel.TVService);

            await Navigation.PushAsync(qualitiesPage);
        }

        private async void Detail_Clicked(object sender, EventArgs e)
        {
            _loggingService.Info($"Detail_Clicked");

            if (PlayingState == PlayingStateEnum.PlayingInternal)
            {
                ShowJustPlayingNotification();
            }
            else
            {
                await _viewModel.ShowPopupMenu(_viewModel.SelectedItem);
            }
        }

        private bool LastKeyLongPressed
        {
            get
            {
                return ((_lastKeyLongPressedTime != DateTime.MinValue) && ((DateTime.Now - _lastKeyLongPressedTime).TotalSeconds < 3));
            }
        }

        protected override bool OnBackButtonPressed()
        {
            // this event is called immediately after Navigation.PopAsync();
            if (_lastPageAppearedTime != DateTime.MinValue && ((DateTime.Now - _lastPageAppearedTime).TotalSeconds < 3))
            {
                // ignoring this event
                return true;
            }

            if (LastKeyLongPressed)
            {
                return false;
            }

            if ((_lastBackPressedTime == DateTime.MinValue) || ((DateTime.Now-_lastBackPressedTime).TotalSeconds>5))
            {

                if (PlayingState == PlayingStateEnum.PlayingInternal || PlayingState == PlayingStateEnum.PlayingInPreview)
                {
                    ActionStop(false);
                    _lastBackPressedTime = DateTime.MinValue;
                } else
                if (PlayingState == PlayingStateEnum.Stopped)
                {
                    MessagingCenter.Send($"Stiskněte ještě jednou pro ukončení", BaseViewModel.ToastMessage);
                    _lastBackPressedTime = DateTime.Now;
                }

                return true;
            } else
            {
                return false;
            }
        }

        private async Task CheckStream()
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                if ((PlayingState == PlayingStateEnum.Stopped) || (PlayingState == PlayingStateEnum.PlayingExternal))
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
                }
                else
                {
                    PreviewVideoBordersFix();

                    NoVideoStackLayout.IsVisible = false;
                    VideoStackLayout.IsVisible = true;
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

                    VideoStackLayout.Layout(rect);
                }
            } catch (Exception ex)
            {
                _loggingService.Error(ex);
            }
        }
    }
}
