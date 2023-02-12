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
using Xamarin.Forms.PlatformConfiguration.iOSSpecific;

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
                .AddItem(KeyboardFocusableItem.CreateFrom("UnpairDevice", new List<View>() { DeactivateButton }))

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
                    (_args.FocusedItem.Name == "DVBStreamerUrl" ||
                    _args.FocusedItem.Name == "StopStream")
                    && (!_viewModel.IsDVBStreamerVisible)
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
                SettingsPageScrollView.ScrollToAsync(0, args.FocusedItem.MaxYPosition - Height / 2, false);
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

            _focusItems.DeFocusAll();
        }

        //public void SendOKButton()
        //{
        //    if (_lastFocusedView == null)
        //        return;

        //    if (_lastFocusedView is Button)
        //    {
        //        if (_lastFocusedView == PayButton)
        //            _viewModel.PayCommand.Execute(null);

        //        if (_lastFocusedView == AboutButton)
        //            _viewModel.AboutCommand.Execute(null);

        //        if (_lastFocusedView == DeactivateButton)
        //            _viewModel.DeactivateSledovaniTVDeviceCommand.Execute(null);
        //    }
        //}


        //public void SelectNextItem()
        //{
        //    if (_lastFocusedView == null)
        //    {
        //        FocusView(TVAPIPicker);
        //    } if (_lastFocusedView == UsernameEntry)
        //    {
        //        FocusView(PasswordEntry);
        //    }
        //    else if (_lastFocusedView == O2TVUsernameEntry)
        //    {
        //        FocusView(O2TVPasswordEntry);
        //    }
        //    else if (_lastFocusedView == DVBStreamerUrlEntry)
        //    {
        //        FocusView(LastChannelAutoPlayPicker);
        //    }
        //    else if (_lastFocusedView == PasswordEntry)
        //    {
        //        FocusView(ShowAdultChannelsSwitch);
        //    }
        //    else if (_lastFocusedView == ShowAdultChannelsSwitch)
        //    {
        //        if (_config.ShowAdultChannels)
        //        {
        //            FocusView(PinEntry);
        //        }
        //        else
        //        {
        //            FocusView(ShowSledovaniPairedDeviceSwitch);
        //        }
        //    }
        //    else if (_lastFocusedView == PinEntry)
        //    {
        //        FocusView(ShowSledovaniPairedDeviceSwitch);
        //    }
        //    else if (_lastFocusedView == ShowSledovaniPairedDeviceSwitch)
        //    {
        //        if (_viewModel.ShowSledovaniPairedDevice)
        //        {
        //            FocusView(DeviceIdEntry);
        //        }
        //        else
        //        {
        //            FocusView(LastChannelAutoPlayPicker);
        //        }
        //    }
        //    else if (_lastFocusedView == DeviceIdEntry)
        //    {
        //        FocusView(DevicePasswordEntry);
        //    }
        //    else if (_lastFocusedView == DevicePasswordEntry)
        //    {
        //        if (_viewModel.ShowUnpairButton)
        //        {
        //            FocusView(DeactivateButton);
        //        }
        //        else
        //        {
        //            FocusView(LastChannelAutoPlayPicker);
        //        }
        //    }
        //    else if (_lastFocusedView == DeactivateButton)
        //    {
        //        FocusView(LastChannelAutoPlayPicker);
        //    }
        //    else if (_lastFocusedView == O2TVPasswordEntry)
        //    {
        //        FocusView(LastChannelAutoPlayPicker);
        //    }
        //    else if (_lastFocusedView == SNEntry)
        //    {
        //        FocusView(LastChannelAutoPlayPicker);
        //    }
        //    else if (_lastFocusedView == LastChannelAutoPlayPicker)
        //    {
        //        FocusView(FontSizePicker);
        //    }
        //    else if (_lastFocusedView == FontSizePicker)
        //    {
        //        FocusView(FullscreenSwitch);
        //    }
        //    else if (_lastFocusedView == FullscreenSwitch)
        //    {
        //        FocusView(UseInternalPlayerSwitch);
        //    }
        //    else if (_lastFocusedView == UseInternalPlayerSwitch)
        //    {
        //        FocusView(PlayOnBackgroundSwitch);
        //    }
        //    else if (_lastFocusedView == PlayOnBackgroundSwitch)
        //    {
        //        if (_viewModel.IsPurchased)
        //        {
        //            FocusView(AboutButton);
        //        }
        //        else
        //        {
        //            FocusView(PayButton);
        //        }
        //    }
        //    else if (_lastFocusedView == PayButton)
        //    {
        //        FocusView(AboutButton);
        //    }
        //    else if (_lastFocusedView == AboutButton)
        //    {
        //        FocusView(TVAPIPicker);
        //    }
        //}
    }
}