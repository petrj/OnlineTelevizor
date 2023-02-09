using LoggerService;
using NLog;
using Plugin.InAppBilling;
using OnlineTelevizor.Models;
using OnlineTelevizor.Services;
using OnlineTelevizor.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace OnlineTelevizor.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class HelpPage : ContentPage, IOnKeyDown
    {
        private HelpViewModel _viewModel;
        private IOnlineTelevizorConfiguration _config;
        private ILoggingService _loggingService;
        private IDialogService _dialogService;

        public HelpPage(ILoggingService loggingService, IOnlineTelevizorConfiguration config, IDialogService dialogService, TVService service)
        {
            InitializeComponent();
            _config = config;
            _loggingService = loggingService;
            _dialogService = dialogService;

            BindingContext = _viewModel = new HelpViewModel(loggingService, config, dialogService, service);
        }

        public async void OnKeyDown(string key, bool longPress)
        {
            _loggingService.Debug($"HelpPage Page OnKeyDown {key}{(longPress ? " (long)" : "")}");

            var keyAction = KeyboardDeterminer.GetKeyAction(key);

            switch (keyAction)
            {
                case KeyboardNavigationActionEnum.Down:
                    await mainSCrollView.ScrollToAsync(mainSCrollView.ScrollX, mainSCrollView.ScrollY + 10*(1+(int)_config.AppFontSize), false);
                    break;

                case KeyboardNavigationActionEnum.Up:
                    await mainSCrollView.ScrollToAsync(mainSCrollView.ScrollX, mainSCrollView.ScrollY - 10*(1+(int)_config.AppFontSize), false);
                    break;

                case KeyboardNavigationActionEnum.Back:
                    await Navigation.PopAsync();
                    break;
            }
        }
    }
}