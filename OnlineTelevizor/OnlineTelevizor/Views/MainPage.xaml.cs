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

        private DateTime _lastNumPressedTime = DateTime.MinValue;
        private string _numberPressed = String.Empty;

        public MainPage(ILoggingService loggingService, IOnlineTelevizorConfiguration config, Context context)
        {
            InitializeComponent();

            _dialogService = new DialogService(this);

            _config = config;
            _context = context;
            _loggingService = loggingService;

            BindingContext = _viewModel = new MainPageViewModel(loggingService, config, _dialogService, context);

            MessagingCenter.Subscribe<string>(this, BaseViewModel.KeyMessage, (key) =>
            {
                OnKeyDown(key);
            });

            ChannelsListView.ItemSelected += ChannelsListView_ItemSelected;
        }

        private void ChannelsListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            ChannelsListView.ScrollTo(_viewModel.SelectedItem, ScrollToPosition.MakeVisible, true);
        }

        public void OnKeyDown(string key)
        {
            switch (key.ToLower())
            {
                case "dpaddown":
                    Task.Run(async () => await _viewModel.SelectNextChannel());
                    break;
                case "dpadup":
                    Task.Run(async () => await _viewModel.SelectPreviousChannel());
                    break;
                case "dpadcenter":
                    if (_viewModel.SelectedItem != null)
                        Task.Run(async () => await _viewModel.PlayStream(_viewModel.SelectedItem.Url));
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
                default:
                    _loggingService.Debug($"Unbound key down: {key}");
                    break;
            }
        }

        private void HandleNumKey(int number)
        {
            if ((DateTime.Now - _lastNumPressedTime).TotalSeconds>1)
            {
                _lastNumPressedTime = DateTime.MinValue;
                _numberPressed = String.Empty;
            }

            _lastNumPressedTime = DateTime.Now;
            _numberPressed += number;

            CrossToastPopUp.Current.ShowToastSuccess(_numberPressed);

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
                        if (_viewModel.SelectedItem != null)
                            await _viewModel.PlayStream(_viewModel.SelectedItem.Url);
                    });
                }

            }).Start();
        }

        public void Reset()
        {
            _viewModel.ResetConnectionCommand.Execute(null);
        }

        public void Refresh()
        {
            _viewModel.RefreshCommand.Execute(null);
        }

        private async void ToolbarItemSettings_Clicked(object sender, EventArgs e)
        {
            var settingsPage = new SettingsPage(_loggingService, _config, _context, _dialogService);
            settingsPage.Disappearing += delegate
            {
                _viewModel.ResetConnectionCommand.Execute(null);
                _viewModel.RefreshCommand.Execute(null);
            };

            await Navigation.PushAsync(settingsPage);
        }

        private async void ToolbarItemQuality_Clicked(object sender, EventArgs e)
        {
            var qualitiesPage = new QualitiesPage(_loggingService, _config, _context, _viewModel.TVService);

            await Navigation.PushAsync(qualitiesPage);
        }

        private async void ToolbarItemFilter_Clicked(object sender, EventArgs e)
        {
            var filterPage = new FilterPage(_loggingService, _config, _context, _viewModel.TVService);

            filterPage.Disappearing += delegate
            {
                _viewModel.RefreshCommand.Execute(null);
            };

            await Navigation.PushAsync(filterPage);
        }

        private async void Channel_Tapped(object sender, ItemTappedEventArgs e)
        {
            var channelItem = e.Item as ChannelItem;
            _viewModel.SelectedItem = channelItem;
            await _viewModel.PlayStream(channelItem.Url);
        }
    }
}
