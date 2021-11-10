using LoggerService;
using NLog;
using Plugin.InAppBilling;
using OnlineTelevizor.Models;
using OnlineTelevizor.Services;
using OnlineTelevizor.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using System.Collections.ObjectModel;

namespace OnlineTelevizor.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SettingsPage : ContentPage
    {
        private SettingsViewModel _viewModel;
        private IOnlineTelevizorConfiguration _config;
        private ILoggingService _loggingService;
        private IDialogService _dialogService;
        private View _lastFocusedView = null;

        public SettingsPage(ILoggingService loggingService, IOnlineTelevizorConfiguration config, IDialogService dialogService, TVService service)
        {
            InitializeComponent();

            _config = config;
            _loggingService = loggingService;
            _dialogService = dialogService;

            BindingContext = _viewModel = new SettingsViewModel(loggingService, config, dialogService, service);

            PlayOnBackgroundSwitch.Toggled += PlayOnBackgroundSwitch_Toggled;
            UseInternalPlayerSwitch.Toggled += PlayOnBackgroundSwitch_Toggled;

            if (Device.RuntimePlatform == Device.UWP)
            {
                UsernameEntry.TextColor = Color.Black;
                UsernameEntry.BackgroundColor = Color.Gray;
                PasswordEntry.TextColor = Color.Black;
                PasswordEntry.BackgroundColor = Color.Gray;
                PinEntry.TextColor = Color.Black;
                PinEntry.BackgroundColor = Color.Gray;

                FontSizeLabel.IsVisible = false;
                FontSizePicker.IsVisible = false;
            }

            MessagingCenter.Subscribe<string>(this, BaseViewModel.RequestBatterySettings, async (sender) =>
            {
                if (await _dialogService.Confirm("Při běhu na pozadí je nutné zajistit, aby se aplikace kvůli optimalizaci baterie neukončovala. Přejít do nastavení?"))
                {
                    MessagingCenter.Send<SettingsPage>(this, BaseViewModel.SetBatterySettings);
                }
            });

            TVAPIPicker.Unfocused += TVAPIPicker_Unfocused;
            LastChannelAutoPlayPicker.Unfocused += LastChannelAutoPlayPicker_Unfocused;
            FontSizePicker.Unfocused += FontSizePicker_Unfocused;
        }

        private void FontSizePicker_Unfocused(object sender, FocusEventArgs e)
        {
            FocusView(ShowAdultChannelsSwitch);
        }

        private void LastChannelAutoPlayPicker_Unfocused(object sender, FocusEventArgs e)
        {
            FocusView(FontSizePicker);
        }

        private void TVAPIPicker_Unfocused(object sender, FocusEventArgs e)
        {
            switch (_viewModel.Config.TVApi)
            {
                case TVAPIEnum.SledovaniTV:
                    FocusView(UsernameEntry);
                    break;
                case TVAPIEnum.KUKI:
                    FocusView(SNEntry);
                    break;
                case TVAPIEnum.O2TV:
                    FocusView(O2TVUsernameEntry);
                    break;
                case TVAPIEnum.DVBStreamer:
                    FocusView(DVBStreamerUrlEntry);
                    break;
            }
        }

        private void PlayOnBackgroundSwitch_Toggled(object sender, ToggledEventArgs e)
        {
            if (_config.PlayOnBackground && _config.InternalPlayer)
            {
                MessagingCenter.Send<SettingsPage>(this, BaseViewModel.CheckBatterySettings);
            }
        }

        public void FillAutoPlayChannels(ObservableCollection<ChannelItem> channels)
        {
            _viewModel.FillAutoPlayChannels(channels);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

#if DVBSTREAMER
            var dvbStreamerEnabled = true;
#else
            var dvbStreamerEnabled = false;
#endif

            if (!dvbStreamerEnabled)
            {
                if (_config.TVApi == TVAPIEnum.DVBStreamer)
                    _viewModel.TVAPIIndex = 0;

                if (TVAPIPicker.Items.Contains("DVB Streamer"))
                    TVAPIPicker.Items.Remove("DVB Streamer");
            }
        }

        private void FocusView(View view)
        {
            view.Focus();
            _lastFocusedView = view;
        }

        public void SelectNextItem()
        {
            if (_lastFocusedView == UsernameEntry)
            {
                FocusView(PasswordEntry);
            }
            else if (_lastFocusedView == PasswordEntry)
            {
                FocusView(PinEntry);
            }
            else if (_lastFocusedView == PinEntry)
            {
                FocusView(LastChannelAutoPlayPicker);
            }
            else if (_lastFocusedView == O2TVUsernameEntry)
            {
                FocusView(O2TVPasswordEntry);
            }
            else if (_lastFocusedView == SNEntry)
            {
                FocusView(LastChannelAutoPlayPicker);
            }
            else if (_lastFocusedView == O2TVPasswordEntry)
            {
                FocusView(LastChannelAutoPlayPicker);
            }
            else if (_lastFocusedView == DVBStreamerUrlEntry)
            {
                FocusView(LastChannelAutoPlayPicker);
            }
            else if (_lastFocusedView == ShowAdultChannelsSwitch)
            {
                FocusView(DoNotSplitScreenOnLandscapeSwitch);
            }
            else if (_lastFocusedView == DoNotSplitScreenOnLandscapeSwitch)
            {
                FocusView(FullscreenSwitch);
            }
            else if (_lastFocusedView == FullscreenSwitch)
            {
                FocusView(UseInternalPlayerSwitch);
            }
            else if (_lastFocusedView == UseInternalPlayerSwitch)
            {
                FocusView(PlayOnBackgroundSwitch);
            }
            else if (_lastFocusedView == PlayOnBackgroundSwitch)
            {
                if (_viewModel.IsPurchased)
                {
                    FocusView(AboutButton);
                } else
                {
                    FocusView(PayButton);
                }
            }
            else if (_lastFocusedView == PayButton)
            {
                FocusView(AboutButton);
            }
            else
            {
                FocusView(TVAPIPicker);
            }
        }
    }
}