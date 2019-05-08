using Android.Content;
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
using Plugin.InAppBilling.Abstractions;

namespace OnlineTelevizor.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        private bool _isPruchased;

        public Command PayCommand { get; set; }

        public SettingsViewModel(ILoggingService loggingService, IOnlineTelevizorConfiguration config, Context context, IDialogService dialogService)
            : base(loggingService, config, dialogService, context)
        {
            _loggingService = loggingService;
            _context = context;
            _dialogService = dialogService;
            Config = config;

            IsPurchased = Config.Purchased;

            PayCommand = new Command(async () => await Pay());
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
                await _dialogService.Information("Připojení k platební službě selhalo.");
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
    }
}
