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
using Android.Content;
using Android.Views;
using Plugin.Toast;
using System.Threading;

namespace OnlineTelevizor.Views
{
    public partial class MainPage : ContentPage
    {
        private MainPageViewModel _viewModel;
        private DialogService _dialogService;
        private Context _context;
        private IOnlineTelevizorConfiguration _config;
        private ILoggingService _loggingService;

        private FilterPage _filterPage;

        private DateTime _lastNumPressedTime = DateTime.MinValue;
        private string _numberPressed = String.Empty;

        public MainPage(ILoggingService loggingService, IOnlineTelevizorConfiguration config, Context context)
        {
            InitializeComponent();

            _dialogService = new DialogService(this);

            _config = config;
            _context = context;
            _loggingService = loggingService;

            _loggingService.Debug($"Initializing MainPage");

            BindingContext = _viewModel = new MainPageViewModel(loggingService, config, _dialogService, context);

            MessagingCenter.Subscribe<string>(this, BaseViewModel.KeyMessage, (key) =>
            {
                OnKeyDown(key);
            });

            ChannelsListView.ItemSelected += ChannelsListView_ItemSelected;

             _filterPage = new FilterPage(_loggingService, _config, _context, _viewModel.TVService);
            _filterPage.Disappearing += delegate
            {
                _viewModel.RefreshCommand.Execute(null);
            };

            MessagingCenter.Subscribe<MainPageViewModel>(this, BaseViewModel.ShowDetailMessage, (sender) =>
            {
                Detail_Clicked(this, null);
            });
        }

        private void ChannelsListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            ChannelsListView.ScrollTo(_viewModel.SelectedItem, ScrollToPosition.MakeVisible, false);
        }

        public void OnKeyDown(string key)
        {
            _loggingService.Debug($"OnKeyDown {key}");

            switch (key.ToLower())
            {
                case "dpaddown":
                case "buttonr1":
                case "s":
                    Task.Run(async () => await OnKeyDown());
                    break;
                case "dpadup":
                case "buttonl1":
                case "w":
                    Task.Run(async () => await OnKeyUp());
                    break;
                case "dpadleft":
                case "pageup":
                case "a":
                    Task.Run(async () => await OnKeyLeft());
                    break;
                case "pagedown":
                case "dpadright":
                case "d":
                    Task.Run(async () => await OnKeyRight());
                    break;
                case "dpadcenter":
                case "space":
                case "buttonr2":
                case "mediaplaypause":
                case "enter":
                        Task.Run(async () => await _viewModel.Play());
                    break;
                case "back":
                    break;
                case "num0":
                    HandleNumKey(0);
                    break;
                case "num1":
                    HandleNumKey(1);
                    break;
                case "num2":
                    HandleNumKey(2);
                    break;
                case "num3":
                    HandleNumKey(3);
                    break;
                case "num4":
                    HandleNumKey(4);
                    break;
                case "num5":
                    HandleNumKey(5);
                    break;
                case "num6":
                    HandleNumKey(6);
                    break;
                case "num7":
                    HandleNumKey(7);
                    break;
                case "num8":
                    HandleNumKey(8);
                    break;
                case "num9":
                    HandleNumKey(9);
                    break;
                case "f5":
                case "del":
                    RefreshOnResume();
                    break;
                case "buttonl2":
                case "info":
                case "guide":
                case "i":
                case "g":
                    Detail_Clicked(this, null);
                    break;
                default:
                    if (_config.DebugMode)
                    {
                        _loggingService.Debug($"Unbound key down: {key}");
                        CrossToastPopUp.Current.ShowCustomToast($"Unbound key down: {key}", "#0000FF", "#FFFFFF");
                    }
                    break;
            }
        }

        private void HandleNumKey(int number)
        {
            _loggingService.Debug($"HandleNumKey {number}");

            if ((DateTime.Now - _lastNumPressedTime).TotalSeconds>1)
            {
                _lastNumPressedTime = DateTime.MinValue;
                _numberPressed = String.Empty;
            }

            _lastNumPressedTime = DateTime.Now;
            _numberPressed += number;

            CrossToastPopUp.Current.ShowCustomToast(_numberPressed, "#0000FF", "#FFFFFF");

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
                            await _viewModel.PlayStream(_viewModel.SelectedItem.Url);
                        }
                    });
                }

            }).Start();
        }

        public void RefreshOnResume()
        {
            _loggingService.Debug($"RefreshOnResume");
            
            _viewModel.RefreshCommand.Execute(null);
        }

        private async void ToolbarItemSettings_Clicked(object sender, EventArgs e)
        {
            _loggingService.Debug($"ToolbarItemSettings_Clicked");

            var settingsPage = new SettingsPage(_loggingService, _config, _context, _dialogService);
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
            _loggingService.Debug($"ToolbarItemQuality_Clicked");

            var qualitiesPage = new QualitiesPage(_loggingService, _config, _context, _viewModel.TVService);

            await Navigation.PushAsync(qualitiesPage);
        }

        private async Task OnKeyLeft()
        {
            await _viewModel.SelectPreviousChannel(10);
        }

        private async Task OnKeyRight()
        {
            await _viewModel.SelectNextChannel(10);
        }

        private async Task OnKeyDown()
        {
             await _viewModel.SelectNextChannel();
        }

        private async Task OnKeyUp()
        {
            await _viewModel.SelectPreviousChannel();
        }

        private async void ToolbarItemFilter_Clicked(object sender, EventArgs e)
        {
            _loggingService.Debug($"ToolbarItemFilter_Clicked");

            await Navigation.PushAsync(_filterPage);
        }

        private async void Detail_Clicked(object sender, EventArgs e)
        {
            _loggingService.Debug($"Detail_Clicked");

            if (_viewModel.SelectedItem != null)
            {
                var detailPage = new ChannelDetailPage(_loggingService, _config, _dialogService, _context);
                detailPage.Channel = _viewModel.SelectedItem;

                await Navigation.PushAsync(detailPage);
            } else
            {
                await _dialogService.Information("Není označen žádný kanál");
            }
        }
    }
}
