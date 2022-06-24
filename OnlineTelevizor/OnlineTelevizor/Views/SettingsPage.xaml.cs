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
    public partial class SettingsPage : ContentPage, INavigationSelectNextItem, INavigationSendOKButton
    {
        private SettingsViewModel _viewModel;
        private IOnlineTelevizorConfiguration _config;
        private ILoggingService _loggingService;
        private IDialogService _dialogService;
        private View _lastFocusedView = null;
        private string _appVersion = String.Empty;

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

            Appearing += delegate
            {
                MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_RequestBatterySettings, async (sender) =>
                {
                    if (await _dialogService.Confirm("Při běhu na pozadí je nutné zajistit, aby se aplikace kvůli optimalizaci baterie neukončovala. Přejít do nastavení?"))
                    {
                        MessagingCenter.Send<SettingsPage>(this, BaseViewModel.MSG_SetBatterySettings);
                    }
                });
            };

            Disappearing += delegate
            {
                MessagingCenter.Unsubscribe<string>(this, BaseViewModel.MSG_RequestBatterySettings);
            };

            TVAPIPicker.Unfocused += TVAPIPicker_Unfocused;
            LastChannelAutoPlayPicker.Unfocused += LastChannelAutoPlayPicker_Unfocused;
            FontSizePicker.Unfocused += FontSizePicker_Unfocused;

            PasswordEntry.Unfocused += PasswordEntry_Unfocused;
        }

        public string AppVersion
        {
            get
            {
                return _appVersion;
            }
            set
            {
                _appVersion = value;
                if (_viewModel != null)
                    _viewModel.AppVersion = value;
            }
        }

        private void PasswordEntry_Unfocused(object sender, FocusEventArgs e)
        {
            //if (_viewModel.Config.IsRunningOnTV)
                FocusView(PinEntry);
        }

        private void FontSizePicker_Unfocused(object sender, FocusEventArgs e)
        {
            //if (_viewModel.Config.IsRunningOnTV)
                FocusView(ShowAdultChannelsSwitch);
        }

        private void LastChannelAutoPlayPicker_Unfocused(object sender, FocusEventArgs e)
        {
            //if (_viewModel.Config.IsRunningOnTV)
                FocusView(FontSizePicker);
        }

        private void TVAPIPicker_Unfocused(object sender, FocusEventArgs e)
        {
            //if (_viewModel.Config.IsRunningOnTV)
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
                MessagingCenter.Send<SettingsPage>(this, BaseViewModel.MSG_CheckBatterySettings);
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
            if (Device.OS == TargetPlatform.Windows)
            {
                return;
            }

            PayButton.BackgroundColor = Color.Gray;
            PayButton.TextColor = Color.Black;

            AboutButton.BackgroundColor = Color.Gray;
            AboutButton.TextColor = Color.Black;

            view.Focus();
            _lastFocusedView = view;

            if (view is Button)
            {
                (view as Button).BackgroundColor = Color.Blue;
                (view as Button).TextColor = Color.White;
            }
        }

        public void SendOKButton()
        {
            if (_lastFocusedView == null)
                return;

            if (_lastFocusedView is Button)
            {
                if (_lastFocusedView == PayButton)
                    _viewModel.PayCommand.Execute(null);

                if (_lastFocusedView == AboutButton)
                    _viewModel.AboutCommand.Execute(null);
            }
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