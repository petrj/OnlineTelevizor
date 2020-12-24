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

        private DateTime _lastNumPressedTime = DateTime.MinValue;
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

            ChannelsListView.ItemSelected += ChannelsListView_ItemSelected;

            _filterPage = new FilterPage(_loggingService, _config, _viewModel.TVService);
            _filterPage.Disappearing += delegate
            {
                _viewModel.RefreshCommand.Execute(null);
            };

            MessagingCenter.Subscribe<MainPageViewModel>(this, BaseViewModel.ShowDetailMessage, (sender) =>
            {
                Detail_Clicked(this, null);
            });

            MessagingCenter.Subscribe<BaseViewModel, string[]>(this, BaseViewModel.PlayInternal, (sender, urlAndNameTypeAndDescription) =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    if (_playerPage == null)
                        _playerPage = new PlayerPage(_loggingService, _config, _dialogService);

                    var playing = _playerPage.Playing;

                    _playerPage.SetMediaUrl(urlAndNameTypeAndDescription[0], urlAndNameTypeAndDescription[1], urlAndNameTypeAndDescription[2], urlAndNameTypeAndDescription[3]);

                    if (!playing)
                    {
                         Navigation.PushModalAsync(_playerPage);
                    }
                });
            });

            MessagingCenter.Subscribe<MainPageViewModel>(this, BaseViewModel.ShowConfiguration, (sender) =>
            {
                ToolbarItemSettings_Clicked(this, null);
            });

            if (Device.RuntimePlatform == Device.UWP ||
                Device.RuntimePlatform == Device.iOS)
            {
                ChannelsListView.ItemTapped += ChannelsListView_ItemTapped;
            }

            ScrollViewChannelEPGDescription.Scrolled += ScrollViewChannelEPGDescription_Scrolled;
        }

        private void ScrollViewChannelEPGDescription_Scrolled(object sender, ScrolledEventArgs e)
        {
            if (_viewModel.SelectedPart != SelectedPartEnum.EPGDetail)
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
        }

        private void ChannelsListView_ItemTapped(object sender, ItemTappedEventArgs e)
        {
            Task.Run(async () => await _viewModel.Play());
        }

        private void ChannelsListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (!_viewModel.DoNotScrollToChannel)
            {
                ChannelsListView.ScrollTo(_viewModel.SelectedItem, ScrollToPosition.MakeVisible, _config.AnimatedScrolling);
            }

            _viewModel.DoNotScrollToChannel = false;
        }

        public void OnKeyDown(string key)
        {
            _loggingService.Debug($"OnKeyDown {key}");

            // key events can be consumed only on this MainPage

            var stack = Navigation.NavigationStack;
            if  (stack[stack.Count - 1].GetType() != typeof(MainPage))
            {
                // different page on navigation top
                return;
            }

            switch (key.ToLower())
            {
                case "dpaddown":
                case "buttonr1":
                case "down":
                case "s":
                    Task.Run(async () => await OnKeyDown());
                    break;
                case "dpadup":
                case "buttonl1":
                case "up":
                case "w":
                    Task.Run(async () => await OnKeyUp());
                    break;
                case "dpadleft":
                case "pageup":
                case "left":
                case "a":
                case "b":
                case "mediaplayprevious":
                    Task.Run(async () => await OnKeyLeft());
                    break;
                case "pagedown":
                case "dpadright":
                case "right":
                case "d":
                case "f":
                case "mediaplaynext":
                    Task.Run(async () => await OnKeyRight());
                    break;
                case "dpadcenter":
                case "space":
                case "buttonr2":
                case "mediaplaypause":
                case "enter":
                        Task.Run(async () => await OnKeyPlay());
                    break;
                //case "back":
                case "f4":
                case "escape":
                case "mediaplaystop":
                    StopPlayback();
                    break;
                case "num0":
                case "number0":
                    HandleNumKey(0);
                    break;
                case "num1":
                case "number1":
                    HandleNumKey(1);
                    break;
                case "num2":
                case "number2":
                    HandleNumKey(2);
                    break;
                case "num3":
                case "number3":
                    HandleNumKey(3);
                    break;
                case "num4":
                case "number4":
                    HandleNumKey(4);
                    break;
                case "num5":
                case "number5":
                    HandleNumKey(5);
                    break;
                case "num6":
                case "number6":
                    HandleNumKey(6);
                    break;
                case "num7":
                case "number7":
                    HandleNumKey(7);
                    break;
                case "num8":
                case "number8":
                    HandleNumKey(8);
                    break;
                case "num9":
                case "number9":
                    HandleNumKey(9);
                    break;
                case "f5":
                case "del":
                    Reset();
                    Refresh();
                    break;
                case "buttonl2":
                case "info":
                case "guide":
                case "i":
                case "g":
                    Detail_Clicked(this, null);
                    break;
                default:
                    {
                        if (_config.DebugMode)
                        {
                            _loggingService.Debug($"Unbound key down: {key}");
                            MessagingCenter.Send($"Unbound key down: {key}", BaseViewModel.ToastMessage);
                        }
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
                            await _viewModel.PlayStream(_viewModel.SelectedItem.Url, _viewModel.SelectedItem.Name, _viewModel.SelectedItem.Type, _viewModel.SelectedItem.CurrentEPGTitle);
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

        private async Task OnKeyPlay()
        {
            if (!Playing)
            {
                await _viewModel.Play();
            }
        }

        private async Task OnKeyLeft()
        {
            if (!Playing)
            {
                if (_viewModel.IsPortrait)
                {
                    // no EPG detail on right
                    await _viewModel.SelectPreviousChannel(10);
                }
                else
                {
                    // EPG detail on right
                    if (_viewModel.SelectedPart == SelectedPartEnum.EPGDetail)
                    {
                        await ScrollViewChannelEPGDescription.ScrollToAsync(0, 0, false);
                        _viewModel.SelectedPart = SelectedPartEnum.ChannelsList;
                    }
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
                if (_viewModel.IsPortrait)
                {
                    // no EPG detail on right
                    await _viewModel.SelectNextChannel(10);
                }
                else
                {
                    // EPG detail on right
                    if (_viewModel.SelectedPart == SelectedPartEnum.ChannelsList)
                    {
                        _viewModel.SelectedPart = SelectedPartEnum.EPGDetail;
                    }
                }
            }
            else
            {
                await _viewModel.SelectNextChannel();
                await _viewModel.Play();
            }
        }

        private async Task OnKeyDown()
        {
            if (!Playing)
            {
                if (_viewModel.SelectedPart == SelectedPartEnum.ChannelsList)
                {
                    await _viewModel.SelectNextChannel();
                } else
                {
                    await ScrollViewChannelEPGDescription.ScrollToAsync(ScrollViewChannelEPGDescription.ScrollX, ScrollViewChannelEPGDescription.ScrollY + 10+(int)_config.AppFontSize, false);
                }
            }
            else
            {
                await _viewModel.SelectNextChannel();
                await _viewModel.Play();
            }
        }

        private async Task OnKeyUp()
        {
            if (!Playing)
            {
                if (_viewModel.SelectedPart == SelectedPartEnum.ChannelsList)
                {
                    await _viewModel.SelectPreviousChannel();
                }
                else
                {
                    await ScrollViewChannelEPGDescription.ScrollToAsync(ScrollViewChannelEPGDescription.ScrollX, ScrollViewChannelEPGDescription.ScrollY - (10 + (int)_config.AppFontSize), false);
                }
            }
            else
            {
                await _viewModel.SelectPreviousChannel();
                await _viewModel.Play();
            }
        }

        private async void ToolbarItemFilter_Clicked(object sender, EventArgs e)
        {
            _loggingService.Info($"ToolbarItemFilter_Clicked");

            await Navigation.PushAsync(_filterPage);
        }

        private async void Detail_Clicked(object sender, EventArgs e)
        {
            _loggingService.Info($"Detail_Clicked");

            if (_viewModel.SelectedItem != null)
            {
                var detailPage = new ChannelDetailPage(_loggingService, _config, _dialogService, _viewModel.TVService);
                detailPage.Channel = _viewModel.SelectedItem;

                await Navigation.PushAsync(detailPage);
            } else
            {
                await _dialogService.Information("Není označen žádný kanál");
            }
        }
    }
}
