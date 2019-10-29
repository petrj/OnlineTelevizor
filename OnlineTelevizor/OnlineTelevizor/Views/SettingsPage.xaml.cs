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
        IDialogService _dialogService;

        public SettingsPage(ILoggingService loggingService, IOnlineTelevizorConfiguration config, IDialogService dialogService, TVService service)
        {
            InitializeComponent();

            _config = config;
            _loggingService = loggingService;
            _dialogService = dialogService;

            BindingContext = _viewModel = new SettingsViewModel(loggingService, config, dialogService, service);

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
    }
}