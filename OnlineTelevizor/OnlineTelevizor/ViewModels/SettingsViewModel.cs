using LoggerService;
using OnlineTelevizor.Models;
using OnlineTelevizor.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Plugin.Permissions;
using Plugin.Permissions.Abstractions;
using Plugin.InAppBilling;
using System.Collections.ObjectModel;
using System.Reflection;
using Plugin.InAppBilling.Abstractions;

namespace OnlineTelevizor.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        private bool _isPruchased;
        private bool _isFullscreen;
        private TVService _service;

        public Command PayCommand { get; set; }
        public Command StopStreamCommand { get; set; }

        public Command AboutCommand { get; set; }

        public ObservableCollection<ChannelItem> AutoPlayChannels { get; set; } = new ObservableCollection<ChannelItem>();

        private ChannelItem _selectedChannelItem;

        public SettingsViewModel(ILoggingService loggingService, IOnlineTelevizorConfiguration config, IDialogService dialogService, TVService service)
            : base(loggingService, config, dialogService)
        {
            _service = service;
            _loggingService = loggingService;
            _dialogService = dialogService;
            Config = config;

            IsPurchased = Config.Purchased;

            PayCommand = new Command(async () => await Pay());
            StopStreamCommand = new Command(async () => await StopStream());
            AboutCommand = new Command(async () => await About());
        }

        public string FontSizeForCaption
        {
            get
            {
                return GetScaledSize(14).ToString();
            }
        }

        public bool IsFullScreen
        {
            get
            {
                return Config.Fullscreen;
            }
            set
            {
                Config.Fullscreen = value;
                if (value)
                {
                    MessagingCenter.Send(String.Empty, BaseViewModel.EnableFullScreen);
                } else
                {
                    MessagingCenter.Send(String.Empty, BaseViewModel.DisableFullScreen);
                }

                OnPropertyChanged(nameof(IsFullScreen));
            }
        }

        public string FontSizeForText
        {
            get
            {
                return GetScaledSize(11).ToString();
            }
        }

        public string FontSizeForEntry
        {
            get
            {
                return GetScaledSize(14).ToString();
            }
        }

        public string FontSizeForPicker
        {
            get
            {
                return GetScaledSize(12).ToString();
            }
        }

        public void NotifyFontSizeChange()
        {
            OnPropertyChanged(nameof(FontSizeForCaption));
            OnPropertyChanged(nameof(FontSizeForEntry));
            OnPropertyChanged(nameof(FontSizeForPicker));
            OnPropertyChanged(nameof(FontSizeForText));
        }

        public void FillAutoPlayChannels(ObservableCollection<ChannelItem> channels = null)
        {
            AutoPlayChannels.Clear();

            var first = new ChannelItem()
            {
                Name = "Nespouštět žádný kanál",
                ChannelNumber = "-1"
            };
            var second = new ChannelItem()
            {
                Name = "Posledně vybraný kanál",
                ChannelNumber = "0"
            };

            AutoPlayChannels.Add(first);
            AutoPlayChannels.Add(second);

            var anythingSelected = false;

            foreach (var ch in channels)
            {
                AutoPlayChannels.Add(ch);

                if (ch.ChannelNumber == Config.AutoPlayChannelNumber)
                {
                    anythingSelected = true;
                    SelectedChannelItem = ch;
                }
            }

            if (!anythingSelected)
            {
                if (Config.AutoPlayChannelNumber == "0")
                {
                    SelectedChannelItem = second;
                }
                else
                {
                    SelectedChannelItem = first;
                }
            }

            OnPropertyChanged(nameof(AutoPlayChannels));
        }

        protected async Task StopStream()
        {
            await _service.StopStream();
        }

        protected async Task About()
        {
            var sb = new StringBuilder();

            sb.AppendLine("");
            sb.AppendLine($"Autor: Petr Janoušek");

            _loggingService.Info($"Checking purchase");
            try
            {
                // contacting service

                var connected = await CrossInAppBilling.Current.ConnectAsync();

                if (connected)
                {
                    // check InAppBillingPurchase
                    var purchases = await CrossInAppBilling.Current.GetPurchasesAsync(ItemType.InAppPurchase);
                    foreach (var purchase in purchases)
                    {
                        if (purchase.ProductId == Config.PurchaseProductId &&
                            purchase.State == PurchaseState.Purchased)
                        {
                            sb.AppendLine("");
                            sb.AppendLine("");
                            sb.AppendLine($"Zakoupena plná verze");
                            sb.AppendLine("");
                            sb.AppendLine($"Datum : {purchase.TransactionDateUtc}");
                            sb.AppendLine($"Id objednávky: {purchase.Id}");

                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "Error while checking purchase");
            }
            finally
            {
                await CrossInAppBilling.Current.DisconnectAsync();
            }

            await _dialogService.Information(sb.ToString(), "Online Televizor");
        }

        protected async Task Pay()
        {
            if (Config.DebugMode)
            {
                Config.Purchased = true;
                IsPurchased = true;
                return;
            }

            try
            {
                _loggingService.Debug($"Paying product id: {Config.PurchaseProductId}");

                var connected = await CrossInAppBilling.Current.ConnectAsync();

                if (!connected)
                {
                    _loggingService.Info($"Connection to AppBilling service failed");
                    await _dialogService.Information("Připojení k platební službě selhalo.");
                    return;
                }

                var purchase = await CrossInAppBilling.Current.PurchaseAsync(Config.PurchaseProductId, ItemType.InAppPurchase, "apppayload");
                if (purchase == null)
                {
                    _loggingService.Info($"Not purchased");
                    //await _dialogService.Information("Platba nebyla uskutečněna.");
                }
                else
                {
                    _loggingService.Info($"Purchase OK");

                    _loggingService.Info($"Purchase Id: {purchase.Id}");
                    _loggingService.Info($"Purchase Token: {purchase.PurchaseToken}");
                    _loggingService.Info($"Purchase State: {purchase.State.ToString()}");
                    _loggingService.Info($"Purchase Date: {purchase.TransactionDateUtc.ToString()}");
                    _loggingService.Info($"Purchase Payload: {purchase.Payload}");
                    _loggingService.Info($"Purchase ConsumptionState: {purchase.ConsumptionState.ToString()}");
                    _loggingService.Info($"Purchase AutoRenewing: {purchase.AutoRenewing}");

                    Config.Purchased = true;
                    IsPurchased = true;

                    //await _dialogService.Information("Platba byla úspěšně provedena.");
                }
            }
            catch (Exception ex)
            {
                //await _dialogService.Information("Platba se nezdařila.");
                _loggingService.Error(ex, "Payment failed");
            }
            finally
            {
                await CrossInAppBilling.Current.DisconnectAsync();
            }
        }

        public int LoggingLevelIndex
        {
            get
            {
                switch (Config.LoggingLevel)
                {
                    case LoggingLevelEnum.Debug:
                        return 0;
                    case LoggingLevelEnum.Info:
                        return 1;
                    case LoggingLevelEnum.Error:
                        return 2;
                }

                return 2;
            }
            set
            {
                // 0 -> Debug
                // 1 -> Info
                // 3 -> Error

                switch (value)
                {
                    case 0:
                        Config.LoggingLevel = LoggingLevelEnum.Debug;
                        break;
                    case 1:
                        Config.LoggingLevel = LoggingLevelEnum.Info;
                        break;
                    case 2:
                        Config.LoggingLevel = LoggingLevelEnum.Error;
                        break;
                }
                OnPropertyChanged(nameof(LoggingLevelIndex));
            }
        }

        public int TVAPIIndex
        {
            get
            {
                return (int)Config.TVApi;
            }
            set
            {
                Config.TVApi = (TVAPIEnum)value;

                OnPropertyChanged(nameof(TVAPIIndex));
                OnPropertyChanged(nameof(IsSledovaniTVVisible));
                OnPropertyChanged(nameof(IsKUKITVVisible));
                OnPropertyChanged(nameof(IsDVBStreamerVisible));
                OnPropertyChanged(nameof(IsO2TVVisible));
            }
        }

        public bool IsSledovaniTVVisible
        {
            get
            {
                return Config.TVApi == TVAPIEnum.SledovaniTV;
            }
        }
        public bool IsKUKITVVisible
        {
            get
            {
                return Config.TVApi == TVAPIEnum.KUKI;
            }
        }

        public bool IsDVBStreamerVisible
        {
            get
            {
                return Config.TVApi == TVAPIEnum.DVBStreamer;
            }
        }

        public bool IsO2TVVisible
        {
            get
            {
                return Config.TVApi == TVAPIEnum.O2TV;
            }
        }

        public int AppFontSizeIndex
        {
            get
            {
                return (int)Config.AppFontSize;
            }
            set
            {
                Config.AppFontSize = (AppFontSizeEnum)value;

                OnPropertyChanged(nameof(AppFontSizeIndex));

                NotifyFontSizeChange();
            }
        }

        public bool IsNotPurchased
        {
            get
            {
                return !IsPurchased;
            }
            set
            {
                IsPurchased = !value;

                OnPropertyChanged(nameof(IsNotPurchased));
                OnPropertyChanged(nameof(IsPurchased));
            }
        }

        public bool IsPurchased
        {
            get
            {
                return _isPruchased;
            }
            set
            {
                _isPruchased = value;

                OnPropertyChanged(nameof(IsNotPurchased));
                OnPropertyChanged(nameof(IsPurchased));
            }
        }

        public ChannelItem SelectedChannelItem
        {
            get
            {
                return _selectedChannelItem;
            }
            set
            {
                _selectedChannelItem = value;

                if (value != null)
                    Config.AutoPlayChannelNumber = value.ChannelNumber;

                OnPropertyChanged(nameof(SelectedChannelItem));
            }
        }
    }
}
