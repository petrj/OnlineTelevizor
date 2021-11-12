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

namespace OnlineTelevizor.Views
{
    public partial class MainPage : ContentPage
    {
        private MainPageViewModel _viewModel;
        private DialogService _dialogService;
        private IOnlineTelevizorConfiguration _config;
        private ILoggingService _loggingService;

        private FilterPage _filterPage;
        private PlayerPage _playerPage;
        private CastRenderersPage _renderersPage;

        private DateTime _lastNumPressedTime = DateTime.MinValue;
        private DateTime _lastBackPressedTime = DateTime.MinValue;
        private DateTime _lastKeyLongPressedTime = DateTime.MinValue;
        private DateTime _lastPageAppearedTime = DateTime.MinValue;
        private bool _firstSelectionAfterStartup = false;
        private string _numberPressed = String.Empty;

        public MainPage(ILoggingService loggingService, IOnlineTelevizorConfiguration config)
        {
            InitializeComponent();

            _dialogService = new DialogService(this);

            _config = config;
            _loggingService = loggingService;

            _loggingService.Debug($"Initializing MainPage");

            BindingContext = _viewModel = new MainPageViewModel(loggingService, config, _dialogService);

            MessagingCenter.Subscribe<string>(this, BaseViewModel.KeyMessage, (key) =>
            {
                OnKeyDown(key);
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.KeyLongMessage, (key) =>
            {
                _lastKeyLongPressedTime = DateTime.Now;
            });

            ChannelsListView.ItemSelected += ChannelsListView_ItemSelected;
            ChannelsListView.Scrolled += ChannelsListView_Scrolled;

            _filterPage = new FilterPage(_loggingService, _config, _viewModel.TVService);
            _filterPage.Disappearing += delegate
            {
                _viewModel.RefreshCommand.Execute(null);
            };

            MessagingCenter.Subscribe<string>(this, BaseViewModel.PlayNext, (msg) =>
            {
                Task.Run(async () =>
                {
                    await _viewModel.SelectNextChannel();
                    await _viewModel.Play();
                });
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.PlayPrevious, (msg) =>
            {
                Task.Run(async () =>
                {
                    await _viewModel.SelectPreviousChannel();
                    await _viewModel.Play();
                });
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

            MessagingCenter.Subscribe<BaseViewModel, MediaDetail>(this, BaseViewModel.PlayInternal, (sender, mediaDetail) =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        if (_playerPage == null)
                            _playerPage = new PlayerPage(_loggingService, _config, _dialogService, _viewModel.TVService);

                        var playing = _playerPage.Playing;

                        _playerPage.SetMediaUrl(mediaDetail);

                        if (!playing)
                        {
                            Navigation.PushModalAsync(_playerPage);
                        }
                    }
                    catch (Exception ex)
                    {
                        _loggingService.Error(ex);
                    }
                });
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
                StopPlayback();
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.StopRecord, async (sender) =>
            {
                await Task.Run(async () =>
                {
                    await _viewModel.RecordChannel(false);
                });
            });

            if (Device.RuntimePlatform == Device.UWP ||
            Device.RuntimePlatform == Device.iOS)
            {
                ChannelsListView.ItemTapped += ChannelsListView_ItemTapped;
            }

            ScrollViewChannelEPGDescription.Scrolled += ScrollViewChannelEPGDescription_Scrolled;
            Appearing += MainPage_Appearing;
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
            base.OnSizeAllocated(width, height);

            _viewModel.SelectedPart = SelectedPartEnum.ChannelsList;

            if (width>height && !_config.DoNotSplitScreenOnLandscape)
            {
                _viewModel.IsPortrait = false;
                LayoutGrid.ColumnDefinitions[0].Width = new GridLength(width/2.0);
                LayoutGrid.ColumnDefinitions[1].Width = new GridLength(width/2.0);
            } else
            {
                _viewModel.IsPortrait = true;

                LayoutGrid.ColumnDefinitions[0].Width = new GridLength(width);
                LayoutGrid.ColumnDefinitions[1].Width = new GridLength(0);
            }

            _viewModel.NotifyToolBarChange();
        }

        private void ChannelsListView_ItemTapped(object sender, ItemTappedEventArgs e)
        {
            Task.Run(async () => await _viewModel.Play());
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

        private static bool LeavePageKey(string lowKey)
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

        private static bool SelectNextItemKey(string lowKey)
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
                    if (LeavePageKey(lowKey))
                    {
                        // closing detail page
                        Navigation.PopAsync();
                    }
                }

                if (stack[stack.Count - 1].GetType() == typeof(FilterPage))
                {
                    if (LeavePageKey(lowKey))
                    {
                        // closing filter page
                        Navigation.PopAsync();
                    }

                    if (_filterPage != null)
                    {
                        if (SelectNextItemKey(lowKey))
                            _filterPage.SelectNextItem();
                    }
                }

                if (stack[stack.Count - 1].GetType() == typeof(QualitiesPage))
                {
                    if (LeavePageKey(lowKey))
                    {
                        // closing quality page
                        Navigation.PopAsync();
                    }

                    var qualityPage = stack[stack.Count - 1] as QualitiesPage;

                    if (SelectNextItemKey(lowKey))
                        qualityPage.SelectNextItem();
                }

                if (stack[stack.Count - 1].GetType() == typeof(SettingsPage))
                {
                    if (LeavePageKey(lowKey))
                    {
                        // closing settings page
                        Navigation.PopAsync();
                    }

                    var settingsPage = stack[stack.Count - 1] as SettingsPage;

                    if (SelectNextItemKey(lowKey))
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
                case "f2":
                case "mediaplaynext":
                case "medianext":
                case "moveend":
                    Task.Run(async () => await OnKeyDown(1));
                    break;
                case "dpadup":
                case "buttonl1":
                case "up":
                case "w":
                case "numpad8":
                case "f3":
                case "mediaplayprevious":
                case "mediaprevious":
                case "movehome":
                    Task.Run(async () => await OnKeyUp(1));
                    break;
                case "pagedown":
                    Task.Run(async () => await OnKeyDown(10));
                    break;
                case "pageup":
                    Task.Run(async () => await OnKeyUp(10));
                    break;
                case "dpadleft":
                case "left":
                case "a":
                case "b":
                case "numpad4":
                case "leftbracket":
                    Task.Run(async () => await OnKeyLeft());
                    break;
                case "dpadright":
                case "right":
                case "d":
                case "f":
                case "numpad6":
                case "rightbracket":
                    Task.Run(async () => await OnKeyRight());
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
                    Task.Run(async () => await OnKeyPlay());
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
                    StopPlayback();
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
                            await _viewModel.PlayStream(new MediaDetail()
                            {
                                MediaUrl = _viewModel.SelectedItem.Url,
                                Title = _viewModel.SelectedItem.Name,
                                Type = _viewModel.SelectedItem.Type,
                                CurrentEPGItem = _viewModel.SelectedItem.CurrentEPGItem,
                                NextEPGItem = _viewModel.SelectedItem.NextEPGItem,
                                ChanneldID = _viewModel.SelectedItem.Id,
                                LogoUrl = _viewModel.SelectedItem.LogoUrl
                            });
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

        public bool Playing
        {
            get
            {
                return _playerPage != null && _playerPage.Playing;
            }
        }

        public void StopPlayback()
        {
            if (Playing)
            {
                _playerPage.Stop();
                Navigation.PopModalAsync();
            }
        }

        public void ResumePlayback()
        {
            if (Playing)
            {
                _playerPage.Resume();
            }
        }

        public void Refresh()
        {
            _loggingService.Info($"Refresh");

            _viewModel.RefreshCommand.Execute(null);
        }

        private async Task OnKeyPlay()
        {
            if (_viewModel.SelectedPart == SelectedPartEnum.ChannelsList ||
                _viewModel.SelectedPart == SelectedPartEnum.EPGDetail)
            {
                if (!Playing)
                {
                    await _viewModel.Play();
                } else
                {
                    _playerPage.ShowJustPlayingNotification();
                }
            }
            else if (_viewModel.SelectedPart == SelectedPartEnum.ToolBar)
            {

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

        private async Task OnKeyLeft()
        {
            if (!Playing)
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
            else
            {
                await _viewModel.SelectPreviousChannel();
                await _viewModel.Play();
            }
        }

        private async Task OnKeyRight()
        {
            if (!Playing)
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
            else
            {
                await _viewModel.SelectNextChannel();
                await _viewModel.Play();
            }
        }

        private async Task OnKeyDown(int step)
        {
            if (!Playing)
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
            else
            {
                await _viewModel.SelectNextChannel(step);
                await _viewModel.Play();
            }
        }

        private async Task OnKeyUp(int step)
        {
            if (!Playing)
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
            else
            {
                await _viewModel.SelectPreviousChannel(step);
                await _viewModel.Play();
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

            if (Playing)
            {
                _playerPage.ShowJustPlayingNotification();
            }
            else
            {
                if (_viewModel.SelectedItem != null)
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        _viewModel.LongPress(_viewModel.SelectedItem);
                    });
                }
                else
                {
                    await _dialogService.Information("Není označen žádný kanál");
                }
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

            if ((_lastKeyLongPressedTime != DateTime.MinValue) && ((DateTime.Now - _lastKeyLongPressedTime).TotalSeconds < 3))
            {
                // long press back
                return false;
            }

            if ((_lastBackPressedTime == DateTime.MinValue) || ((DateTime.Now-_lastBackPressedTime).TotalSeconds>5))
            {
                MessagingCenter.Send($"Stiskněte ještě jednou pro ukončení", BaseViewModel.ToastMessage);
                _lastBackPressedTime = DateTime.Now;
                return true;
            } else
            {
                return false;
            }
        }
    }
}
