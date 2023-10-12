using LoggerService;
using OnlineTelevizor.Models;
using OnlineTelevizor.Services;
using OnlineTelevizor.ViewModels;
using System;
using System.Collections.Generic;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using System.Collections.ObjectModel;
using System.Data;
using System.Net;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace OnlineTelevizor.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SettingsPage : ContentPage, IOnKeyDown
    {
        private SettingsViewModel _viewModel;
        private IOnlineTelevizorConfiguration _config;
        private ILoggingService _loggingService;
        private IDialogService _dialogService;
        private string _appVersion = String.Empty;
        private KeyboardFocusableItemList _focusItems;

        public SettingsPage(ILoggingService loggingService, IOnlineTelevizorConfiguration config, IDialogService dialogService, TVService service)
        {
            InitializeComponent();

            _config = config;
            _loggingService = loggingService;
            _dialogService = dialogService;

            BindingContext = _viewModel = new SettingsViewModel(loggingService, config, dialogService, service);

            PlayOnBackgroundSwitch.Toggled += PlayOnBackgroundSwitch_Toggled;
            UseInternalPlayerSwitch.Toggled += PlayOnBackgroundSwitch_Toggled;
            ShowSledovaniPairedDeviceSwitch.Toggled += ShowSledovaniPairedDeviceSwitch_Toggled;
            IPEntry.Unfocused += IPEntry_Unfocused;
            PortEntry.Unfocused += PortEntry_Unfocused;

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

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_RequestBatterySettings, async (sender) =>
            {
                if (await _dialogService.Confirm("Při běhu na pozadí je nutné zajistit, aby se aplikace kvůli optimalizaci baterie neukončovala. Přejít do nastavení?"))
                {
                    MessagingCenter.Send<SettingsPage>(this, BaseViewModel.MSG_SetBatterySettings);
                }
            });

            BuildFocusableItems();
        }

        private async void PortEntry_Unfocused(object sender, FocusEventArgs e)
        {
            if (PortEntry.Text == null || string.IsNullOrEmpty(PortEntry.Text))
            {
                await _dialogService.Information("Port musí být vyplněn");
                PortEntry.Text = "49152";
                return;
            }

            int port;
            if (!int.TryParse(PortEntry.Text, out port))
            {
                await _dialogService.Information("Neplatné číslo portu");
                PortEntry.Text = "49152";
                return;
            }

            if (port<0 || port > 65535)
            {
                await _dialogService.Information("Neplatné číslo portu");
                PortEntry.Text = "49152";
            }
        }

        private async void IPEntry_Unfocused(object sender, FocusEventArgs e)
        {
            if (IPEntry.Text == null || string.IsNullOrEmpty(IPEntry.Text))
            {
                await _dialogService.Information("IP adresa nemůže být prázdná");
                try
                {
                    var ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
                    IPEntry.Text = ipHostInfo.AddressList[0].ToString();
                }
                catch
                {
                    IPEntry.Text = "192.168.1.10";
                }
            }
        }

        private void BuildFocusableItems()
        {
            _focusItems = new KeyboardFocusableItemList();

            _focusItems
                .AddItem(KeyboardFocusableItem.CreateFrom("ActiveIPTV", new List<View>() { ActiveIPTVBoxView, TVAPIPicker }))

                .AddItem(KeyboardFocusableItem.CreateFrom("SledovaniTVUserName", new List<View>() { SledovaniTVUserNameBoxView, UsernameEntry }))
                .AddItem(KeyboardFocusableItem.CreateFrom("SledovaniTVPassword", new List<View>() { SledovaniTVPasswordBoxView, PasswordEntry }))
                .AddItem(KeyboardFocusableItem.CreateFrom("SledovaniTVShowAdultChannels", new List<View>() { SledovaniTVShowAdultChannelsBoxView, ShowAdultChannelsSwitch }))
                .AddItem(KeyboardFocusableItem.CreateFrom("SledovaniTVPIN", new List<View>() { SledovaniTVPINBoxView, PinEntry }))
                .AddItem(KeyboardFocusableItem.CreateFrom("ShowPairedCredentials", new List<View>() { ShowPairedCredentialsBoxView, ShowSledovaniPairedDeviceSwitch }))
                .AddItem(KeyboardFocusableItem.CreateFrom("SledovaniTVDeviceId", new List<View>() { SledovaniTVDeviceIdBoxView, DeviceIdEntry }))
                .AddItem(KeyboardFocusableItem.CreateFrom("SledovaniTVDevicePassword", new List<View>() { SledovaniTVDevicePasswordBoxView, DevicePasswordEntry }))
                .AddItem(KeyboardFocusableItem.CreateFrom("UnpairDevice", new List<View>() { SledovaniTVUnpairButton }))

                .AddItem(KeyboardFocusableItem.CreateFrom("KUKISN", new List<View>() { KUKISNBoxView, SNEntry }))

                .AddItem(KeyboardFocusableItem.CreateFrom("DVBStreamerUrl", new List<View>() { DVBStreamerUrlBoxView, DVBStreamerUrlEntry }))
                .AddItem(KeyboardFocusableItem.CreateFrom("StopStream", new List<View>() { StopStreamButton }))

                .AddItem(KeyboardFocusableItem.CreateFrom("O2Username", new List<View>() { O2UsernameBoxView, O2TVUsernameEntry }))
                .AddItem(KeyboardFocusableItem.CreateFrom("O2Password", new List<View>() { O2PasswordBoxView, O2TVPasswordEntry }))

                .AddItem(KeyboardFocusableItem.CreateFrom("AutoPlay", new List<View>() { AutoPlayBoxView, LastChannelAutoPlayPicker }))
                .AddItem(KeyboardFocusableItem.CreateFrom("FontSize", new List<View>() { FontSizeBoxView, FontSizePicker }))
                .AddItem(KeyboardFocusableItem.CreateFrom("Fullscreen", new List<View>() { FullscreenBoxView, FullscreenSwitch }))
                .AddItem(KeyboardFocusableItem.CreateFrom("PlayInternal", new List<View>() { PlayInternalBoxView, UseInternalPlayerSwitch }))
                .AddItem(KeyboardFocusableItem.CreateFrom("PlayOnBackground", new List<View>() { PlayOnBackgroundBoxView, PlayOnBackgroundSwitch }))

                .AddItem(KeyboardFocusableItem.CreateFrom("RemoteAccessEnabled", new List<View>() { RemoteAccessEnabledBoxView, RemoteAccessSwitch }))
                .AddItem(KeyboardFocusableItem.CreateFrom("RemoteAccessIP", new List<View>() { RemoteAccessIPBoxView, IPEntry }))
                .AddItem(KeyboardFocusableItem.CreateFrom("RemoteAccessPort", new List<View>() { RemoteAccessPortBoxView, PortEntry }))
                .AddItem(KeyboardFocusableItem.CreateFrom("RemoteAccessSecurityKey", new List<View>() { RemoteAccessSecurityKeyBoxView, SecurityKeyEntry }))

                .AddItem(KeyboardFocusableItem.CreateFrom("Pay", new List<View>() { PayButton }))
                .AddItem(KeyboardFocusableItem.CreateFrom("About", new List<View>() { AboutButton }));

            _focusItems.OnItemFocusedEvent += _focusItems_OnItemFocusedEvent;

            _focusItems.OnItemFocusedEvent += SettingsPage_OnItemFocusedEvent;
        }

        private void _focusItems_OnItemFocusedEvent(KeyboardFocusableItemEventArgs _args)
        {
            if (_args.FocusedItem == null)
                return;

            Action action = null;

            switch (_focusItems.LastFocusDirection)
            {
                case KeyboardFocusDirection.Next: action = delegate { _focusItems.FocusNextItem(); }; break;
                case KeyboardFocusDirection.Previous: action = delegate { _focusItems.FocusPreviousItem(); }; break;
                default: action = delegate { _args.FocusedItem.DeFocus(); }; break;
            }

            if (_args.FocusedItem.Name == "Pay" && (!_viewModel.IsNotPurchased))
            {
                action();
                return;
            }

            if (_args.FocusedItem.Name == "KUKISN" && (!_viewModel.IsKUKITVVisible))
            {
                action();
                return;
            }

            if (
                    (_args.FocusedItem.Name == "O2Username" ||
                    _args.FocusedItem.Name == "O2Password")
                    && (!_viewModel.IsO2TVVisible)
               )
            {
                action();
                return;
            }

            if (
                    (_args.FocusedItem.Name == "RemoteAccessPort" ||
                    _args.FocusedItem.Name == "RemoteAccessSecurityKey" ||
                    _args.FocusedItem.Name == "RemoteAccessIP")
                    && (!_config.AllowRemoteAccessService)
               )
            {
                action();
                return;
            }

            if
                (
                    (_args.FocusedItem.Name == "SledovaniTVUserName" ||
                    _args.FocusedItem.Name == "SledovaniTVPassword" ||
                    _args.FocusedItem.Name == "SledovaniTVShowAdultChannels" ||
                    _args.FocusedItem.Name == "SledovaniTVPIN" ||
                    _args.FocusedItem.Name == "ShowPairedCredentials" ||
                    _args.FocusedItem.Name == "SledovaniTVDeviceId" ||
                    _args.FocusedItem.Name == "SledovaniTVDevicePassword" ||
                    _args.FocusedItem.Name == "UnpairDevice")
                    && (!_viewModel.IsSledovaniTVVisible)
                )
            {
                action();
                return;
            }

            switch (_args.FocusedItem.Name)
            {
                case "SledovaniTVPIN":
                    if (!_viewModel.IsPINShowed)
                    {
                        action();
                    }
                    break;

                case "SledovaniTVDeviceId":
                case "SledovaniTVDevicePassword":
                    if (!_viewModel.ShowSledovaniPairedDevice)
                    {
                        action();
                    }
                    break;

                case "UnpairDevice":
                    if (!_viewModel.ShowUnpairButton || !_viewModel.ShowSledovaniPairedDevice)
                    {
                        action();
                    }
                    break;
            }
        }

        private void SettingsPage_OnItemFocusedEvent(KeyboardFocusableItemEventArgs args)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                // scroll to element
                await SettingsPageScrollView.ScrollToAsync(0, args.FocusedItem.MaxYPosition - args.FocusedItem.Height, false);
            });
        }

        public async void OnKeyDown(string key, bool longPress)
        {
            _loggingService.Debug($"FilterPage Page OnKeyDown {key}{(longPress ? " (long)" : "")}");

            var keyAction = KeyboardDeterminer.GetKeyAction(key);

            switch (keyAction)
            {
                case KeyboardNavigationActionEnum.Right:
                case KeyboardNavigationActionEnum.Down:
                    _focusItems.FocusNextItem();
                    break;
                case KeyboardNavigationActionEnum.Left:
                case KeyboardNavigationActionEnum.Up:
                        _focusItems.FocusPreviousItem();
                    break;

                case KeyboardNavigationActionEnum.Back:
                    await Navigation.PopAsync();
                    break;

                case KeyboardNavigationActionEnum.OK:
                    switch (_focusItems.FocusedItemName)
                    {
                        case "ActiveIPTV":
                            TVAPIPicker.Focus();
                            break;
                        case "SledovaniTVShowAdultChannels":
                            ShowAdultChannelsSwitch.IsToggled = !ShowAdultChannelsSwitch.IsToggled;
                            break;
                        case "SledovaniTVUserName":
                            UsernameEntry.Focus();
                            break;
                        case "SledovaniTVPassword":
                            PasswordEntry.Focus();
                            break;
                        case "SledovaniTVPIN":
                            PinEntry.Focus();
                            break;
                        case "ShowPairedCredentials":
                            ShowSledovaniPairedDeviceSwitch.IsToggled = !ShowSledovaniPairedDeviceSwitch.IsToggled;
                            break;
                        case "SledovaniTVDeviceId":
                            DeviceIdEntry.Focus();
                            break;
                        case "SledovaniTVDevicePassword":
                            DevicePasswordEntry.Focus();
                            break;
                        case "UnpairDevice":
                            _viewModel.DeactivateSledovaniTVDeviceCommand.Execute(null);
                            break;
                        case "KUKISN":
                            SNEntry.Focus();
                            break;
                        case "DVBStreamerUrl":
                            DVBStreamerUrlEntry.Focus();
                            break;
                        case "StopStream":
                            _viewModel.StopStreamCommand.Execute(null);
                            break;
                        case "O2Username":
                            O2TVUsernameEntry.Focus();
                            break;
                        case "O2Password":
                            O2TVPasswordEntry.Focus();
                            break;
                        case "AutoPlay":
                            LastChannelAutoPlayPicker.Focus();
                            break;
                        case "FontSize":
                            FontSizePicker.Focus();
                            break;
                        case "Fullscreen":
                            FullscreenSwitch.IsToggled = !FullscreenSwitch.IsToggled;
                            break;
                        case "PlayInternal":
                            UseInternalPlayerSwitch.IsToggled = !UseInternalPlayerSwitch.IsToggled;
                            break;
                        case "PlayOnBackground":
                            PlayOnBackgroundSwitch.IsToggled = !PlayOnBackgroundSwitch.IsToggled;
                            break;

                        case "RemoteAccessEnabled":
                            RemoteAccessSwitch.IsToggled = !RemoteAccessSwitch.IsToggled;
                            break;
                        case "RemoteAccessPort":
                            PortEntry.Focus();
                            break;
                        case "RemoteAccessSecurityKey":
                            SecurityKeyEntry.Focus();
                            break;
                        case "RemoteAccessIP":
                            IPEntry.Focus();
                            break;


                        case "Pay":
                            _viewModel.PayCommand.Execute(null);
                            break;
                        case "About":
                            _viewModel.AboutCommand.Execute(null);
                            break;
                    }
                    break;
            }
        }

        public void OnTextSent(string text)
        {
            switch (_focusItems.FocusedItemName)
            {
                case "SledovaniTVUserName":
                    UsernameEntry.Text = text;
                    break;
                case "SledovaniTVPassword":
                    PasswordEntry.Text = text;
                    break;
                case "SledovaniTVPIN":
                    PinEntry.Text = text;
                    break;
                case "SledovaniTVDeviceId":
                    DeviceIdEntry.Text = text;
                    break;
                case "SledovaniTVDevicePassword":
                    DevicePasswordEntry.Text = text;
                    break;
                case "KUKISN":
                    SNEntry.Text = text;
                    break;
                case "DVBStreamerUrl":
                    DVBStreamerUrlEntry.Text = text;
                    break;
                case "O2Username":
                    O2TVUsernameEntry.Text = text;
                    break;
                case "O2Password":
                    O2TVPasswordEntry.Text = text;
                    break;
                case "RemoteAccessPort":
                    PortEntry.Text = text;
                    break;
                case "RemoteAccessSecurityKey":
                    SecurityKeyEntry.Text = text;
                    break;
                case "RemoteAccessIP":
                    IPEntry.Text = text;
                    break;
            }
        }

        public void UnsubscribeMessages()
        {
            MessagingCenter.Unsubscribe<string>(this, BaseViewModel.MSG_RequestBatterySettings);
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

        private void ShowSledovaniPairedDeviceSwitch_Toggled(object sender, ToggledEventArgs e)
        {
            if (!e.Value)
                return;

            Device.BeginInvokeOnMainThread( async () =>
            {
                await _dialogService.Information("Přístupové údaje spárovaného zařízení upravujte jen v případě, že opravdu víte, co děláte. Změna údajů může způsobit deaktivaci zařízení! Zařízení je sice možné znovu aktivovat, počet nových aktivací za měsíc je ale omezen.");
            });
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
            _loggingService.Debug("Appearing");

            base.OnAppearing();

            _viewModel.NotifySledovaniTVDeviceIChange();

            _focusItems.DeFocusAll();
        }

        private void OnRemoteTelevizorLabelTapped(object sender, EventArgs e)
        {
            Task.Run(async () => await Launcher.OpenAsync("https://play.google.com/store/apps/details?id=net.petrjanousek.RemoteTelevizor"));
        }
    }
}