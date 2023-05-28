using LoggerService;
using OnlineTelevizor.Models;
using OnlineTelevizor.Services;
using OnlineTelevizor.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration.iOSSpecific;
using Xamarin.Forms.Xaml;

namespace OnlineTelevizor.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class TimerPage : ContentPage, IOnKeyDown
    {
        private IOnlineTelevizorConfiguration _config;
        private ILoggingService _loggingService;
        private IDialogService _dialogService;
        private TimerPageViewModel _viewModel;
        private KeyboardFocusableItemList _focusItems;

        public TimerPage(ILoggingService loggingService, IOnlineTelevizorConfiguration config, IDialogService dialogService)
        {
            InitializeComponent();

            _config = config;
            _loggingService = loggingService;
            _dialogService = dialogService;

            BindingContext = _viewModel = new TimerPageViewModel(loggingService, config, dialogService);

            BuildFocusableItems();
        }

        private void BuildFocusableItems()
        {
            _focusItems = new KeyboardFocusableItemList();

            _focusItems
                .AddItem(KeyboardFocusableItem.CreateFrom("Minus", new List<View>() { MinusButton }))
                .AddItem(KeyboardFocusableItem.CreateFrom("Plus", new List<View>() { PlusButton }));
        }

        public async void OnKeyDown(string key, bool longPress)
        {
            var keyAction = KeyboardDeterminer.GetKeyAction(key);

            switch (keyAction)
            {
                case KeyboardNavigationActionEnum.Right:
                case KeyboardNavigationActionEnum.Left:
                case KeyboardNavigationActionEnum.Down:
                case KeyboardNavigationActionEnum.Up:
                    _focusItems.FocusNextItem();
                    break;

                case KeyboardNavigationActionEnum.Back:
                    await Navigation.PopAsync();
                    break;

                case KeyboardNavigationActionEnum.OK:

                        switch (_focusItems.FocusedItemName)
                        {
                            case "Minus":
                                DecreaseTime();
                                break;

                            case "Plus":
                                IncreaseTime();
                                break;
                        }

                    break;
            }
        }

        public void OnTextSent(string text)
        {
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            _viewModel.TimerMinutes = 0;
        }

        private void IncreaseTime()
        {
            if (_viewModel.TimerMinutes<50)
            {
                _viewModel.TimerMinutes += 5;
            } else
            {
                _viewModel.TimerMinutes += 10;
            }
        }

        private void DecreaseTime()
        {
            if (_viewModel.TimerMinutes < 50)
            {
                _viewModel.TimerMinutes -= 5;
            }
            else
            {
                _viewModel.TimerMinutes -= 10;
            }
        }

        public Decimal TimerMinutes
        {
            get
            {
                return _viewModel.TimerMinutes;
            }
        }
    }
}